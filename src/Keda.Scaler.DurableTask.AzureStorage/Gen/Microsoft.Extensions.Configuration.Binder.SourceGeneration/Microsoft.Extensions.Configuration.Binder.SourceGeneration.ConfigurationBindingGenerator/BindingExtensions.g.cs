﻿// <auto-generated/>

#nullable enable annotations
#nullable disable warnings

// Suppress warnings about [Obsolete] member usage in generated code.
#pragma warning disable CS0612, CS0618

namespace System.Runtime.CompilerServices
{
    using System;
    using System.CodeDom.Compiler;

    [GeneratedCode("Microsoft.Extensions.Configuration.Binder.SourceGeneration", "9.0.11.2809")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    file sealed class InterceptsLocationAttribute : Attribute
    {
        public InterceptsLocationAttribute(int version, string data)
        {
        }
    }
}

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.CompilerServices;

    [GeneratedCode("Microsoft.Extensions.Configuration.Binder.SourceGeneration", "9.0.11.2809")]
    file static class BindingExtensions
    {
        #region IConfiguration extensions.
        /// <summary>Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.</summary>
        [InterceptsLocation(1, "oiyq+Ek3YuXoDnLhLQtleEsGAABJQ29uZmlndXJhdGlvbi5FeHRlbnNpb25zLmNz")] // C:\Git\durabletask-azurestorage-scaler\src\Keda.Scaler.DurableTask.AzureStorage\Certificates\IConfiguration.Extensions.cs(38,81)
        public static void Bind_ClientCertificateValidationOptions(this IConfiguration configuration, object? instance)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            if (instance is null)
            {
                return;
            }

            var typedObj = (global::Keda.Scaler.DurableTask.AzureStorage.Certificates.ClientCertificateValidationOptions)instance;
            BindCore(configuration, ref typedObj, defaultValueIfNotFound: false, binderOptions: null);
        }

        /// <summary>Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.</summary>
        [InterceptsLocation(1, "94vOHmC4xoCv1REOzGNIGbEDAABDb25maWd1cmVTY2FsZXJPcHRpb25zLmNz")] // C:\Git\durabletask-azurestorage-scaler\src\Keda.Scaler.DurableTask.AzureStorage\Metadata\ConfigureScalerOptions.cs(24,16)
        public static void Bind_ScalerOptions(this IConfiguration configuration, object? instance)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            if (instance is null)
            {
                return;
            }

            var typedObj = (global::Keda.Scaler.DurableTask.AzureStorage.Metadata.ScalerOptions)instance;
            BindCore(configuration, ref typedObj, defaultValueIfNotFound: false, binderOptions: null);
        }
        #endregion IConfiguration extensions.

        #region OptionsBuilder<TOptions> extensions.
        /// <summary>Registers the dependency injection container to bind <typeparamref name="TOptions"/> against the <see cref="IConfiguration"/> obtained from the DI service provider.</summary>
        [InterceptsLocation(1, "/3MNeE1JY/xkE9t5VlNxzzYEAABJU2VydmljZUNvbGxlY3Rpb24uRXh0ZW5zaW9ucy5jcw==")] // C:\Git\durabletask-azurestorage-scaler\src\Keda.Scaler.DurableTask.AzureStorage\Certificates\IServiceCollection.Extensions.cs(26,14)
        [InterceptsLocation(1, "/3MNeE1JY/xkE9t5VlNxz+0FAABJU2VydmljZUNvbGxlY3Rpb24uRXh0ZW5zaW9ucy5jcw==")] // C:\Git\durabletask-azurestorage-scaler\src\Keda.Scaler.DurableTask.AzureStorage\Certificates\IServiceCollection.Extensions.cs(34,14)
        public static OptionsBuilder<TOptions> BindConfiguration<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, string configSectionPath, Action<BinderOptions>? configureBinder = null) where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            ArgumentNullException.ThrowIfNull(configSectionPath);

            optionsBuilder.Configure<IConfiguration>((instance, config) =>
            {
                ArgumentNullException.ThrowIfNull(config);

                IConfiguration section = string.Equals(string.Empty, configSectionPath, StringComparison.OrdinalIgnoreCase) ? config : config.GetSection(configSectionPath);
                BindCoreMain(section, instance, typeof(TOptions), configureBinder);
            });

            optionsBuilder.Services.AddSingleton<IOptionsChangeTokenSource<TOptions>, ConfigurationChangeTokenSource<TOptions>>(sp =>
            {
                return new ConfigurationChangeTokenSource<TOptions>(optionsBuilder.Name, sp.GetRequiredService<IConfiguration>());
            });

            return optionsBuilder;
        }
        #endregion OptionsBuilder<TOptions> extensions.

        #region Core binding extensions.
        private readonly static Lazy<HashSet<string>> s_configKeys_CaCertificateFileOptions = new(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Path", "ReloadDelayMs" });
        private readonly static Lazy<HashSet<string>> s_configKeys_ClientCertificateValidationOptions = new(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Enable", "CertificateAuthority", "RevocationMode" });
        private readonly static Lazy<HashSet<string>> s_configKeys_CertificateValidationCacheOptions = new(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CacheEntryExpiration", "CacheSize" });
        private readonly static Lazy<HashSet<string>> s_configKeys_ScalerOptions = new(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AccountName", "ClientId", "Cloud", "Connection", "ConnectionFromEnv", "EndpointSuffix", "EntraEndpoint", "MaxActivitiesPerWorker", "MaxOrchestrationsPerWorker", "TaskHubName", "UseTablePartitionManagement", "UseManagedIdentity" });

        public static void BindCoreMain(IConfiguration configuration, object instance, Type type, Action<BinderOptions>? configureOptions)
        {
            if (instance is null)
            {
                return;
            }

            if (!HasValueOrChildren(configuration))
            {
                return;
            }

            BinderOptions? binderOptions = GetBinderOptions(configureOptions);

            if (type == typeof(global::Keda.Scaler.DurableTask.AzureStorage.Certificates.ClientCertificateValidationOptions))
            {
                var temp = (global::Keda.Scaler.DurableTask.AzureStorage.Certificates.ClientCertificateValidationOptions)instance;
                BindCore(configuration, ref temp, defaultValueIfNotFound: false, binderOptions);
                return;
            }
            else if (type == typeof(global::Microsoft.AspNetCore.Authentication.Certificate.CertificateValidationCacheOptions))
            {
                var temp = (global::Microsoft.AspNetCore.Authentication.Certificate.CertificateValidationCacheOptions)instance;
                BindCore(configuration, ref temp, defaultValueIfNotFound: false, binderOptions);
                return;
            }

            throw new NotSupportedException($"Unable to bind to type '{type}': generator did not detect the type as input.");
        }

        public static void BindCore(IConfiguration configuration, ref global::Keda.Scaler.DurableTask.AzureStorage.Certificates.CaCertificateFileOptions instance, bool defaultValueIfNotFound, BinderOptions? binderOptions)
        {
            ValidateConfigurationKeys(typeof(global::Keda.Scaler.DurableTask.AzureStorage.Certificates.CaCertificateFileOptions), s_configKeys_CaCertificateFileOptions, configuration, binderOptions);

            if (configuration["Path"] is string value2)
            {
                instance.Path = value2;
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.Path;
                if (currentValue is not null)
                {
                    instance.Path = currentValue;
                }
            }

            if (configuration["ReloadDelayMs"] is string value3 && !string.IsNullOrEmpty(value3))
            {
                instance.ReloadDelayMs = ParseInt(value3, configuration.GetSection("ReloadDelayMs").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.ReloadDelayMs = instance.ReloadDelayMs;
            }
        }

        public static void BindCore(IConfiguration configuration, ref global::Keda.Scaler.DurableTask.AzureStorage.Certificates.ClientCertificateValidationOptions instance, bool defaultValueIfNotFound, BinderOptions? binderOptions)
        {
            ValidateConfigurationKeys(typeof(global::Keda.Scaler.DurableTask.AzureStorage.Certificates.ClientCertificateValidationOptions), s_configKeys_ClientCertificateValidationOptions, configuration, binderOptions);

            if (configuration["Enable"] is string value4 && !string.IsNullOrEmpty(value4))
            {
                instance.Enable = ParseBool(value4, configuration.GetSection("Enable").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.Enable = instance.Enable;
            }

            if (AsConfigWithChildren(configuration.GetSection("CertificateAuthority")) is IConfigurationSection section5)
            {
                global::Keda.Scaler.DurableTask.AzureStorage.Certificates.CaCertificateFileOptions? temp7 = instance.CertificateAuthority;
                temp7 ??= new global::Keda.Scaler.DurableTask.AzureStorage.Certificates.CaCertificateFileOptions();
                BindCore(section5, ref temp7, defaultValueIfNotFound: false, binderOptions);
                instance.CertificateAuthority = temp7;
            }
            else
            {
                instance.CertificateAuthority = instance.CertificateAuthority;
            }

            if (configuration["RevocationMode"] is string value8 && !string.IsNullOrEmpty(value8))
            {
                instance.RevocationMode = ParseEnum<global::System.Security.Cryptography.X509Certificates.X509RevocationMode>(value8, configuration.GetSection("RevocationMode").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.RevocationMode = instance.RevocationMode;
            }
        }

        public static void BindCore(IConfiguration configuration, ref global::Microsoft.AspNetCore.Authentication.Certificate.CertificateValidationCacheOptions instance, bool defaultValueIfNotFound, BinderOptions? binderOptions)
        {
            ValidateConfigurationKeys(typeof(global::Microsoft.AspNetCore.Authentication.Certificate.CertificateValidationCacheOptions), s_configKeys_CertificateValidationCacheOptions, configuration, binderOptions);

            if (configuration["CacheEntryExpiration"] is string value9 && !string.IsNullOrEmpty(value9))
            {
                instance.CacheEntryExpiration = ParseSystemTimeSpan(value9, configuration.GetSection("CacheEntryExpiration").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.CacheEntryExpiration = instance.CacheEntryExpiration;
            }

            if (configuration["CacheSize"] is string value10 && !string.IsNullOrEmpty(value10))
            {
                instance.CacheSize = ParseInt(value10, configuration.GetSection("CacheSize").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.CacheSize = instance.CacheSize;
            }
        }

        public static void BindCore(IConfiguration configuration, ref global::Keda.Scaler.DurableTask.AzureStorage.Metadata.ScalerOptions instance, bool defaultValueIfNotFound, BinderOptions? binderOptions)
        {
            ValidateConfigurationKeys(typeof(global::Keda.Scaler.DurableTask.AzureStorage.Metadata.ScalerOptions), s_configKeys_ScalerOptions, configuration, binderOptions);

            if (configuration["AccountName"] is string value11)
            {
                instance.AccountName = value11;
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.AccountName;
                if (currentValue is not null)
                {
                    instance.AccountName = currentValue;
                }
            }

            if (configuration["ClientId"] is string value12)
            {
                instance.ClientId = value12;
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.ClientId;
                if (currentValue is not null)
                {
                    instance.ClientId = currentValue;
                }
            }

            if (configuration["Cloud"] is string value13)
            {
                instance.Cloud = value13;
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.Cloud;
                if (currentValue is not null)
                {
                    instance.Cloud = currentValue;
                }
            }

            if (configuration["Connection"] is string value14)
            {
                instance.Connection = value14;
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.Connection;
                if (currentValue is not null)
                {
                    instance.Connection = currentValue;
                }
            }

            if (configuration["ConnectionFromEnv"] is string value15)
            {
                instance.ConnectionFromEnv = value15;
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.ConnectionFromEnv;
                if (currentValue is not null)
                {
                    instance.ConnectionFromEnv = currentValue;
                }
            }

            if (configuration["EndpointSuffix"] is string value16)
            {
                instance.EndpointSuffix = value16;
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.EndpointSuffix;
                if (currentValue is not null)
                {
                    instance.EndpointSuffix = currentValue;
                }
            }

            if (configuration["EntraEndpoint"] is string value17 && !string.IsNullOrEmpty(value17))
            {
                instance.EntraEndpoint = ParseSystemUri(value17, configuration.GetSection("EntraEndpoint").Path);
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.EntraEndpoint;
                if (currentValue is not null)
                {
                    instance.EntraEndpoint = currentValue;
                }
            }

            if (configuration["MaxActivitiesPerWorker"] is string value18 && !string.IsNullOrEmpty(value18))
            {
                instance.MaxActivitiesPerWorker = ParseInt(value18, configuration.GetSection("MaxActivitiesPerWorker").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.MaxActivitiesPerWorker = instance.MaxActivitiesPerWorker;
            }

            if (configuration["MaxOrchestrationsPerWorker"] is string value19 && !string.IsNullOrEmpty(value19))
            {
                instance.MaxOrchestrationsPerWorker = ParseInt(value19, configuration.GetSection("MaxOrchestrationsPerWorker").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.MaxOrchestrationsPerWorker = instance.MaxOrchestrationsPerWorker;
            }

            if (configuration["TaskHubName"] is string value20)
            {
                instance.TaskHubName = value20;
            }
            else if (defaultValueIfNotFound)
            {
                var currentValue = instance.TaskHubName;
                if (currentValue is not null)
                {
                    instance.TaskHubName = currentValue;
                }
            }

            if (configuration["UseTablePartitionManagement"] is string value21 && !string.IsNullOrEmpty(value21))
            {
                instance.UseTablePartitionManagement = ParseBool(value21, configuration.GetSection("UseTablePartitionManagement").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.UseTablePartitionManagement = instance.UseTablePartitionManagement;
            }

            if (configuration["UseManagedIdentity"] is string value22 && !string.IsNullOrEmpty(value22))
            {
                instance.UseManagedIdentity = ParseBool(value22, configuration.GetSection("UseManagedIdentity").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.UseManagedIdentity = instance.UseManagedIdentity;
            }
        }


        /// <summary>If required by the binder options, validates that there are no unknown keys in the input configuration object.</summary>
        public static void ValidateConfigurationKeys(Type type, Lazy<HashSet<string>> keys, IConfiguration configuration, BinderOptions? binderOptions)
        {
            if (binderOptions?.ErrorOnUnknownConfiguration is true)
            {
                List<string>? temp = null;
        
                foreach (IConfigurationSection section in configuration.GetChildren())
                {
                    if (!keys.Value.Contains(section.Key))
                    {
                        (temp ??= new List<string>()).Add($"'{section.Key}'");
                    }
                }
        
                if (temp is not null)
                {
                    throw new InvalidOperationException($"'ErrorOnUnknownConfiguration' was set on the provided BinderOptions, but the following properties were not found on the instance of {type}: {string.Join(", ", temp)}");
                }
            }
        }

        public static bool HasValueOrChildren(IConfiguration configuration)
        {
            if ((configuration as IConfigurationSection)?.Value is not null)
            {
                return true;
            }
            return AsConfigWithChildren(configuration) is not null;
        }

        public static IConfiguration? AsConfigWithChildren(IConfiguration configuration)
        {
            foreach (IConfigurationSection _ in configuration.GetChildren())
            {
                return configuration;
            }
            return null;
        }

        public static BinderOptions? GetBinderOptions(Action<BinderOptions>? configureOptions)
        {
            if (configureOptions is null)
            {
                return null;
            }
        
            BinderOptions binderOptions = new();
            configureOptions(binderOptions);
        
            if (binderOptions.BindNonPublicProperties)
            {
                throw new NotSupportedException($"The configuration binding source generator does not support 'BinderOptions.BindNonPublicProperties'.");
            }
        
            return binderOptions;
        }

        public static T ParseEnum<T>(string value, string? path) where T : struct
        {
            try
            {
                return Enum.Parse<T>(value, ignoreCase: true);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{path}' to type '{typeof(T)}'.", exception);
            }
        }

        public static bool ParseBool(string value, string? path)
        {
            try
            {
                return bool.Parse(value);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{path}' to type '{typeof(bool)}'.", exception);
            }
        }

        public static int ParseInt(string value, string? path)
        {
            try
            {
                return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{path}' to type '{typeof(int)}'.", exception);
            }
        }

        public static global::System.TimeSpan ParseSystemTimeSpan(string value, string? path)
        {
            try
            {
                return global::System.TimeSpan.Parse(value, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{path}' to type '{typeof(global::System.TimeSpan)}'.", exception);
            }
        }

        public static global::System.Uri ParseSystemUri(string value, string? path)
        {
            try
            {
                return new Uri(value, UriKind.RelativeOrAbsolute);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{path}' to type '{typeof(global::System.Uri)}'.", exception);
            }
        }
        #endregion Core binding extensions.
    }
}
