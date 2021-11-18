﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dotnetCampus.Cli;
using dotnetCampus.Cli.Standard;
using dotnetCampus.SourceYard.Cli;
using dotnetCampus.SourceYard.Context;
using dotnetCampus.SourceYard.Utils;

namespace dotnetCampus.SourceYard
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MagicTransformMultiTargetingToFirstTarget(args);

            CommandLine.Parse(args).AddStandardHandlers()
                .AddHandler<Options>(RunOptionsAndReturnExitCode)
                .Run();
        }

        private static void MagicTransformMultiTargetingToFirstTarget(string[] args)
        {
            var argDict = dotnetCampus.Cli.CommandLine.Parse(args).ToDictionary(
                            x => x.Key,
                            x => x.Value?.FirstOrDefault()?.Trim());
            var targetFrameworks = argDict["TargetFrameworks"];
            if (targetFrameworks != null && !string.IsNullOrWhiteSpace(argDict["TargetFrameworks"]))
            {
                var first = targetFrameworks.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first))
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = args[i].Replace("\\SourcePacking\\", $"\\{first}\\SourcePacking\\");
                    }
                }
            }
        }

        private static void RunOptionsAndReturnExitCode(Options options)
        {
#if DEBUG
            Debugger.Launch();
            Console.WriteLine(Environment.CommandLine);
#endif
            var logger = new Logger();

            try
            {
                logger.Message("Source packaging");

                var projectFile = options.ProjectFile;
                var multiTargetingPackageInfo = new MultiTargetingPackageInfo(new DirectoryInfo(options.MultiTargetingPackageInfoFolder));
                var intermediateDirectory = GetIntermediateDirectory(multiTargetingPackageInfo, options, logger);
                // 当前多个不同的框架引用不同的文件还不能支持，因此随意获取一个打包文件夹即可
                // 为什么？逻辑上不好解决，其次，安装的项目的兼容性无法处理
                // 安装的项目的兼容性无法处理？源代码包有 net45 框架，项目是 net47 框架，如何让项目能兼容使用到 net45 框架？当前没有此生成逻辑 
                var sourcePackingFolder = GetCommonSourcePackingFolder(multiTargetingPackageInfo, options, logger);
                var packageOutputPath = options.PackageOutputPath;
                var packageVersion = options.PackageVersion;

//                logger.Message($@"项目文件 {projectFile}
//临时文件{intermediateDirectory}
//输出文件{packageOutputPath}
//版本{packageVersion}
//编译的文件{options.CompileFile}
//资源文件{options.ResourceFile}
//内容{options.ContentFile}
//页面{options.Page}
//SourcePackingDirectory: {options.SourcePackingDirectory}");

                var description = ReadFile(options.DescriptionFile);
                var copyright = ReadFile(options.CopyrightFile);

                var buildProps = new BuildProps(logger)
                {
                    Authors = options.Authors ?? string.Empty,
                    Company = options.Company ?? string.Empty,
                    Owner = options.Owner ?? options.Authors ?? string.Empty,
                    Copyright = copyright,
                    Description = description,
                    //PackageProjectUrl = options.PackageProjectUrl,
                    //RepositoryType = options.RepositoryType,
                    //RepositoryUrl = options.RepositoryUrl,
                    Title = options.Title,
                    PackageIconUrl = options.PackageIconUrl,
                    PackageLicenseUrl = options.PackageLicenseUrl,
                    PackageReleaseNotes = options.PackageReleaseNotesFile,
                    PackageTags = options.PackageTags
                };

                buildProps.SetSourcePackingDirectory(sourcePackingFolder.FullName);

                var packer = new Packer
                (
                    projectFile: projectFile,
                    intermediateDirectory: intermediateDirectory,
                    packageOutputPath: packageOutputPath,
                    packageVersion: packageVersion,
                    // 不再从 options 读取，多个框架的情况下，需要各自获取
                    //compileFile: options.CompileFile,
                    //resourceFile: options.ResourceFile,
                    //contentFile: options.ContentFile,
                    //page: options.Page,
                    //applicationDefinition: options.ApplicationDefinition,
                    //noneFile: options.None,
                    //embeddedResource: options.EmbeddedResource,
                    packageId: options.PackageId,
                    buildProps: buildProps,
                    commonSourcePackingFolder: sourcePackingFolder,
                    multiTargetingPackageInfo: multiTargetingPackageInfo,
                    // 多框架下，每个框架有自己的引用路径
                    //packageReferenceVersion: options.PackageReferenceVersion
                    logger: logger
                );

                packer.Pack();
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }

            string ReadFile(string? file)
            {
                if (string.IsNullOrEmpty(file))
                {
                    return "";
                }

                file = Path.GetFullPath(file!);
                if (File.Exists(file))
                {
                    try
                    {
                        return File.ReadAllText(file);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.Message);
                    }
                }

                return "";
            }
        }

        /// <summary>
        /// 获取通用的 SourcePacking 文件夹，无视框架版本的不同
        /// </summary>
        /// <returns></returns>
        private static DirectoryInfo GetCommonSourcePackingFolder(MultiTargetingPackageInfo multiTargetingPackageInfo, Options options, Logger logger)
        {
            // 获取时，需要判断文件夹是否合法
            var fileList = multiTargetingPackageInfo.TargetFrameworkPackageInfoList.Where(t=>t.IsValid).ToList();

            if (fileList.Count == 0)
            {
                logger.Error($"Can not find any TargetFramework info file from \"{multiTargetingPackageInfo.MultiTargetingPackageInfoFolder.FullName}\"");
                Exit(-1);
            }

            // 如果是单个框架的项目，那么返回即可
            if (fileList.Count == 1)
            {
                return fileList[0].SourcePackingFolder;
            }
            else
            {
                if (!string.IsNullOrEmpty(options.TargetFrameworks))
                {
                    var sourcePackingFolder = fileList.FirstOrDefault(t=> options.TargetFrameworks!.Contains(t.TargetFramework))?.SourcePackingFolder;

                    if (sourcePackingFolder is null)
                    {
                        logger.Error($"没有找到匹配框架的打包文件 TargetFrameworks={options.TargetFrameworks};SourceTargetFrameworks={string.Join(";", fileList.Select(t => t.TargetFramework))}");
                        Exit(-1);

                        // 在上面的 Exit 将会退出
                        throw new ArgumentException();
                    }

                    return sourcePackingFolder;
                }
                else
                {
                    return fileList[0].SourcePackingFolder;
                }
            }
        }

        private static string GetIntermediateDirectory(MultiTargetingPackageInfo multiTargetingPackageInfo,Options options, Logger logger)
        {
            // 多框架和单个框架的逻辑不相同
            var folder = options.MultiTargetingPackageInfoFolder;
            folder = Path.GetFullPath(folder);
            const string packageName = "Package";

            if (string.IsNullOrEmpty(options.TargetFrameworks))
            {
                // 单个框架的项目
                var sourcePackingFolder = GetCommonSourcePackingFolder(multiTargetingPackageInfo, options, logger);
                // 预期是输出 obj\Debug\SourcePacking\Package 文件
                var intermediateFolder = Path.Combine(sourcePackingFolder.FullName, packageName);
                return intermediateFolder;
            }
            else
            {
                // 多个框架的项目
                // 输出 obj\Debug\SourceYardMultiTargetingPackageInfoFolder\Package 文件夹
                return Path.Combine(folder, packageName);
            }
        }

        private static void Exit(int code)
        {
            Environment.Exit(code);
        }
    }
}
