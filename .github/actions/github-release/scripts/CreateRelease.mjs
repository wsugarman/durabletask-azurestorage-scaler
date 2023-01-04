const cp = await import('node:child_process');
const fs = await import('node:fs');
const path = await import('node:path');

export default async function createRelease({ github, context, release }) {
  try {
    // Try to get the tag
    await github.rest.git.getRef({
      owner: context.repo.owner,
      repo: context.repo.repo,
      ref: `tags/${release.tag}`
    });

    console.log(`Tag ${release.tag} already exists.`);
    return;
  }
  catch (httpError) {
    // A missing tag will throw a 404
    if (httpError.status == 404) {
      console.log(`Tag ${release.tag} does not yet exist.`);
    } else {
      throw httpError;
    }
  }

  // Create the tag for the release
  const commit = await github.rest.git.getCommit({
    owner: context.repo.owner,
    repo: context.repo.repo,
    commit_sha: context.sha
  });

  await github.rest.git.createTag({
    owner: context.repo.owner,
    repo: context.repo.repo,
    tag: release.tag,
    message: `${release.name} Version ${release.version}`,
    object: context.sha,
    type: 'commit',
    tagger: commit.author
  });

  console.log(`Created tag ${release.tag}.`);

  await github.rest.git.createRef({
    owner: context.repo.owner,
    repo: context.repo.repo,
    ref: `refs/tags/${release.tag}`,
    sha: context.sha
  });

  // Compress the release asset(s) depending on whether the path is a folder and single file
  var assetArchivePath;
  var assetContentType;
  if (fs.lstatSync(release.asset).isDirectory()) {
    // Use zip for folders
    const folderName = path.basename(release.asset);
    assetContentType = 'application/zip';
    assetArchivePath = path.join(release.asset, '..', `${folderName}.zip`);

    const zipOutput = cp.execSync(`zip -r ../${folderName}.zip .`, { cwd: release.asset, encoding: 'utf8' });
    process.stdout.write(zipOutput);

    console.log(`Created zip archive ${path.basename(assetArchivePath)}.`);
  } else {
    // Use gzip for single files
    const file = path.basename(release.asset);
    const directory = path.dirname(release.asset);
    assetContentType = 'application/gzip';
    assetArchivePath = path.join(directory, `${file}.gz`);

    const gzipOutput = cp.execSync(`gzip -9 ${file}`, { cwd: directory, encoding: 'utf8' });
    process.stdout.write(gzipOutput);

    console.log(`Created gzip archive ${path.basename(assetArchivePath)}.`);
  }

  // Create the release
  const newRelease = await github.rest.repos.createRelease({
    owner: context.repo.owner,
    repo: context.repo.repo,
    tag_name: release.tag,
    name: `${release.name} ${release.version}`,
    prerelease: release.prerelease,
    draft: true
  });

  console.log(`Created new release for version ${release.version}.`);

  await github.rest.repos.uploadReleaseAsset({
    owner: context.repo.owner,
    repo: context.repo.repo,
    release_id: newRelease.data.id,
    name: path.basename(assetArchivePath),
    data: fs.readFileSync(assetArchivePath),
    headers: {
      'content-type': assetContentType,
      'content-length': fs.statSync(assetArchivePath).size,
    }
  });

  console.log(`Uploaded release asset ${path.basename(assetArchivePath)}.`);
}
