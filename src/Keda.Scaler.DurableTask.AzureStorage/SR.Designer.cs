﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Keda.Scaler.DurableTask.AzureStorage {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SR {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SR() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Keda.Scaler.DurableTask.AzureStorage.SR", typeof(SR).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple Azure Storage connection values may not be specified..
        /// </summary>
        internal static string AmbiguousConnection {
            get {
                return ResourceManager.GetString("AmbiguousConnection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple identity-based connection options may be specified..
        /// </summary>
        internal static string AmbiguousCredential {
            get {
                return ResourceManager.GetString("AmbiguousCredential", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value cannot be empty or white space..
        /// </summary>
        internal static string EmptyOrWhiteSpace {
            get {
                return ResourceManager.GetString("EmptyOrWhiteSpace", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to find the specified file at &apos;{0}&apos;..
        /// </summary>
        internal static string FileNotFoundFormat {
            get {
                return ResourceManager.GetString("FileNotFoundFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There was an internal server error. Please check the logs for details and ensure your deployment is correct..
        /// </summary>
        internal static string InternalServerError {
            get {
                return ResourceManager.GetString("InternalServerError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The connection environment variable &apos;{0}&apos; could not be resolved..
        /// </summary>
        internal static string InvalidConnectionEnvironmentVariableFormat {
            get {
                return ResourceManager.GetString("InvalidConnectionEnvironmentVariableFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected member of type &apos;{0}&apos; but instead found &apos;{1}&apos;..
        /// </summary>
        internal static string InvalidMemberTypeFormat {
            get {
                return ResourceManager.GetString("InvalidMemberTypeFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot specify a custom TLS client certificate issuer if client certificate validation is disabled..
        /// </summary>
        internal static string InvalidTlsClientValidation {
            get {
                return ResourceManager.GetString("InvalidTlsClientValidation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A key was specified without a certificate..
        /// </summary>
        internal static string MissingCertificate {
            get {
                return ResourceManager.GetString("MissingCertificate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value must be specified with an identity-based connection option like UseWorkloadIdentity..
        /// </summary>
        internal static string MissingCredentialOption {
            get {
                return ResourceManager.GetString("MissingCredentialOption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing value for member &apos;{0}&apos;..
        /// </summary>
        internal static string MissingMemberFormat {
            get {
                return ResourceManager.GetString("MissingMemberFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing required value for private cloud..
        /// </summary>
        internal static string MissingPrivateCloudValue {
            get {
                return ResourceManager.GetString("MissingPrivateCloudValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value may only be specified for private clouds..
        /// </summary>
        internal static string PrivateCloudOnlyValue {
            get {
                return ResourceManager.GetString("PrivateCloudOnlyValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value may only be specified with an Azure Storage account name..
        /// </summary>
        internal static string ServiceUriOnlyValue {
            get {
                return ResourceManager.GetString("ServiceUriOnlyValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown value &apos;{0}&apos;..
        /// </summary>
        internal static string UnknownValueFormat {
            get {
                return ResourceManager.GetString("UnknownValueFormat", resourceCulture);
            }
        }
    }
}
