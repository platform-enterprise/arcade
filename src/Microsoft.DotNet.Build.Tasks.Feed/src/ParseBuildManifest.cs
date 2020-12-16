// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Arcade.Common;
using Microsoft.Build.Framework;
using Microsoft.DotNet.VersionTools.BuildManifest.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using MSBuild = Microsoft.Build.Utilities;

namespace Microsoft.DotNet.Build.Tasks.Feed
{
    /// <summary>
    /// The intended use of this task is to push artifacts described in
    /// a build manifest to a static package feed.
    /// </summary>
    public class ParseBuildManifest : MSBuildTaskBase
    {
        private const string NuGetPackageInfoId = "PackageId";
        private const string NuGetPackageInfoVersion = "PackageVersion";

        /// <summary>
        /// Full path to the assets to publish manifest.
        /// </summary>
        [Required]
        public string AssetManifestPath { get; set; }

        [Output]
        public ITaskItem[] BlobInfos { get; set; }

        [Output]
        public ITaskItem[] PackageInfos { get; set; }

        public override void ConfigureServices(IServiceCollection collection)
        {
            collection.TryAddSingleton<ISigningInformationModelFactory, SigningInformationModelFactory>();
            collection.TryAddSingleton<IBlobArtifactModelFactory, BlobArtifactModelFactory>();
            collection.TryAddSingleton<IPackageArtifactModelFactory, PackageArtifactModelFactory>();
            collection.TryAddSingleton<IBuildModelFactory, BuildModelFactory>();
        }

        public bool ExecuteTask(IBuildModelFactory buildModelFactory)
        {
            Log.LogMessage(MessageImportance.High, "Parsing build manifest file: {0}", AssetManifestPath);
            try
            {
                BuildModel buildModel = buildModelFactory.ManifestFileToModel(AssetManifestPath, Log);
                if (!Log.HasLoggedErrors)
                {
                    if (buildModel.Artifacts.Blobs.Any())
                    {
                        BlobInfos = buildModel.Artifacts.Blobs.Select(blob => new MSBuild.TaskItem(blob.Id)).ToArray();
                    }
                    if (buildModel.Artifacts.Packages.Any())
                    {
                        PackageInfos = buildModel.Artifacts.Packages.Select(ConvertToPackageInfoItem).ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true);
            }
            return !Log.HasLoggedErrors;
        }

        private ITaskItem ConvertToPackageInfoItem(PackageArtifactModel identity)
        {
            var metadata = new Dictionary<string, string>
            {
                [NuGetPackageInfoId] = identity.Id,
                [NuGetPackageInfoVersion] = identity.Version.ToString()
            };
            return new MSBuild.TaskItem(identity.ToString(), metadata);
        }
    }
}
