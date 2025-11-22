using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Xml.Linq;
using Humanizer;
using Ihelpers.Helpers;
using ChoETL;
using System.Text;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
//Test

/// <summary>
/// For this Cli, we use Microsoft.Extensions.Configuration.CommandLine (7.0.0)
/// 
/// Into Package Manager Console you must Write!
/// cd .\CliPlatform
/// 
/// Or if you want to create a new Module and a multiple entities!
///2//--> //dotnet run -- -command moduleScaffold -modulename icommerce -entities stock,item,article
/// 
/// to add entities into a module
///3//--> dotnet run -- -command entityScaffold -entities cart,wishlist -modulename icommerce
///
/// /// to add entities into a module without Services (true or false) -> default is true if no write -services option
///4//--> dotnet run -- -command entityScaffold -entities cart,wishList -modulename icommerce -services true
/// 
/// /// to Create Entities Documentation From specific Module with entities
///5//--> dotnet run -- -command entityDocu  -modulename icommerce -entities cart,wishList
///
/// /// to Create Entities Documentation From specific Module with out entities
///6//--> dotnet run -- -command entityDocu  -modulename icommerce 
///
/// </summary>


class Program
{
    static void Main(string[] args)
    {
        // Get the root path of the project
        string rootPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName);
        Console.WriteLine("Ruta of the Project to create Module: " + rootPath);

        // Creating ConfigurationBuilder object
        var configBuilder = new ConfigurationBuilder();

        //args = new string[] { "-command", "moduleScaffold", "-modulename", "icommerce", "-entities", "stock,item,article", "--" }; // FOR DEBUG..
        // Creating diccionary options
        var options = new Dictionary<string, string>()
                {
                    { "-command", "command" },
                    { "-modulename", "module" },
                    { "-entities", "entities" },
                    { "-services", "services" },
                    { "-caching", "caching" }
                };
        // Add the command line arguments to the configuration using the options dictionary
        configBuilder.AddCommandLine(args, options);
        // Construct the IConfiguration object
        var config = configBuilder.Build();

        // Check if the command is moduleScaffold
        string? command = config["command"];
        string? moduleName = config["module"];
        string[] entities = (config["entities"] ?? "").Split(",");
        string? services = config["services"];
        string? caching = config["caching"];

        //Console.WriteLine($"\n Command {command}");
        //Console.WriteLine($"\n moduleName {moduleName}");
        //Console.WriteLine($"\n entities {entities}");
        //Console.WriteLine($"\n services {services}")
        ////Console.WriteLine($"\n caching {caching}");


        switch (command)
        {
            case "moduleScaffold":
                createModule(moduleName, rootPath);

                if (entities != null && entities.Length > 0 && !String.IsNullOrEmpty(entities[0]))
                {
                    //string listEntities = String.Join(", ", entities);
                    //Console.WriteLine($"\n !VER SI PASA LISTA! ENTIDADES: {listEntities}");
                    creaeteEntities(moduleName, entities, rootPath, ConvertToBool(services), ConvertToBool(caching));
                }
                Console.WriteLine($"INFO MESSAGE!: into Platform you can seed your module, move up to if(runSeeders) your {moduleName}Seeder.Seed(app); and execute your program");
                break;
            case "entityScaffold":
                creaeteEntities(moduleName, entities, rootPath, ConvertToBool(services), ConvertToBool(caching));
                break;
            case "entityDocu":
                creaeteEntityDocumentation(moduleName, entities, rootPath);
                break;
            default:
                break;
        }
                
        static void createModule(string moduleName, string rootPath)
        {
            bool moduleExist = verifyModule(moduleName, rootPath);
            //string rootPath = "D:\\imagina\\.net\\netcore\\CliPlatform"; FOR DEBUG...
            #region Module Name
            if (!string.IsNullOrEmpty(moduleName) && !moduleExist)
            {
                // Capitalize module name
                moduleName = StringHelper.Capitalize(moduleName.Trim());

                var modulePath = Path.Combine(rootPath, moduleName);
                Directory.CreateDirectory(modulePath);
                Console.WriteLine($"Created folder {modulePath}");

                // Create webapp project with module name
                var process1 = new System.Diagnostics.Process();
                process1.StartInfo.FileName = "dotnet";
                process1.StartInfo.Arguments = $"new webapp -n {moduleName}";
                process1.StartInfo.WorkingDirectory = rootPath;
                process1.Start();


                #region Basic Config Folder and Files

                //Creating Config Folder
                var configRootModule = rootPath + "\\" + moduleName;
                Console.WriteLine($"Root Entity: {configRootModule}");
                var configFolderPath = Path.Combine(configRootModule, "Config");
                Directory.CreateDirectory(configFolderPath);

                string[] resourceNames = {
                "CliPlatform.Templates.BasicModules.ConfigsTemplate.CmsPages.txt",
                "CliPlatform.Templates.BasicModules.ConfigsTemplate.CmsSidebar.txt",
                "CliPlatform.Templates.BasicModules.ConfigsTemplate.Permissions.txt",
                "CliPlatform.Templates.BasicModules.ConfigsTemplate.Configs.txt",
                "CliPlatform.Templates.BasicModules.ConfigsTemplate.Settings.txt"
            };

                string[] files = { "CmsPages.cs", "CmsSidebar.cs", "Permissions.cs", "Configs.cs", "Settings.cs" };

                for (int i = 0; i < resourceNames.Length; i++)
                {
                    creatingModuleFromTxtFile(resourceNames[i], files[i], moduleName, configFolderPath);
                }
                #endregion

                #region Creating Folder and Files
                //Creating Data Folder
                var dataRootModule = rootPath + "\\" + moduleName;
                Console.WriteLine($"Root Module to Data: {dataRootModule}");
                var dataFolderPath = Path.Combine(dataRootModule, "Data");
                Directory.CreateDirectory(dataFolderPath);

                //Creating Seeder Folder into Data
                var seederRootRepository = rootPath + "\\" + moduleName + "\\" + "Data";
                Console.WriteLine($"Root Module to Seeder: {seederRootRepository}");
                var seederFolderPathRepository = Path.Combine(seederRootRepository, "Seeders");
                Directory.CreateDirectory(seederFolderPathRepository);

                //Creating Module Seeder File into Seeder Folder
                var lowerModuleName = moduleName.First().ToString().ToLower() + moduleName.Substring(1);
                var moduleSeeder = "CliPlatform.Templates.BasicModules.SeedersTemplate.ModuleSeeder.txt";
                creatingModuleFromTxtFile(moduleSeeder, moduleName + "ModuleSeeder.cs", moduleName, seederFolderPathRepository, lowerModuleName);

                //Creating Seed File into Seeder Folder
                var classEntitySeeder = "CliPlatform.Templates.BasicModules.SeedersTemplate.Seeder.txt";
                creatingModuleFromTxtFile(classEntitySeeder, moduleName + "Seeder.cs", moduleName, seederFolderPathRepository);

                //Creating ModuleConfigurationExtension File into Data Folder
                var moduleConfigurationExtension = "CliPlatform.Templates.BasicModules.ModuleConfigurationExtensionTemplate.txt";
                creatingModuleFromTxtFile(moduleConfigurationExtension, moduleName + "ConfigurationExtension.cs", moduleName, dataRootModule);

                /////Creating Controllers Empty Folder
                var controllerModule = rootPath + "\\" + moduleName;
                Console.WriteLine($"Root Module to Controllers: {controllerModule}");
                var controllerFolderPath = Path.Combine(controllerModule, "Controllers");
                Directory.CreateDirectory(controllerFolderPath);

                /////Creating Services Empty Folder
                var servicesModule = rootPath + "\\" + moduleName;
                Console.WriteLine($"Root Module to Data: {servicesModule}");
                var servicesFolderPath = Path.Combine(servicesModule, "Services");
                Directory.CreateDirectory(servicesFolderPath);

                /////Creating Repositories Empty Folder
                var repositoriesModule = rootPath + "\\" + moduleName;
                Console.WriteLine($"Root Module to Data: {repositoriesModule}");
                var repositoriesFolderPath = Path.Combine(repositoriesModule, "Repositories");
                Directory.CreateDirectory(repositoriesFolderPath);
                #endregion

                /////Creating Caching Empty Folder
                var cachingModule = rootPath + "\\" + moduleName + "\\" + "Repositories";
                Console.WriteLine($"Root Module to Data: {cachingModule}");
                var cachingFolderPath = Path.Combine(cachingModule, "Caching");
                Directory.CreateDirectory(servicesFolderPath);

                #region Deleting unnecesary Module Folders and Files
                //Deleting unnecesary Module Folders
                DeleteFolder("Pages", dataRootModule);
                DeleteFolder("wwwroot", dataRootModule);

                //Deleting unnecesary Module Files
                DeleteFile("Program.cs", dataRootModule);
                DeleteFile("appsettings.json", dataRootModule);
                DeleteFile("appsettings.Development.json", dataRootModule);
                #endregion

                //Add to Virtual Modules Folder the new moduleName WebApp Project
                var processVirtualFolder = new System.Diagnostics.Process();
                processVirtualFolder.StartInfo.FileName = "dotnet";
                processVirtualFolder.StartInfo.Arguments = $"sln Platform.sln add {rootPath}\\{moduleName}\\{moduleName}.csproj --solution-folder Modules\\{moduleName}";
                processVirtualFolder.StartInfo.WorkingDirectory = rootPath;
                processVirtualFolder.Start();

                // Wait until procees has finished
                while (!processVirtualFolder.HasExited)
                {
                    Thread.Sleep(1000);
                }

                //Add project references
                var processReference = new System.Diagnostics.Process();
                processReference.StartInfo.FileName = "dotnet";
                processReference.StartInfo.Arguments = $"add {rootPath}\\{moduleName}\\{moduleName}.csproj reference " +
                                                       $"{rootPath}\\Idata\\Idata.csproj " +
                                                       $"{rootPath}\\Icomment\\Icomment.csproj " +
                                                       $"{rootPath}\\Core\\Core.csproj " +
                                                       $"{rootPath}\\Isite\\Isite.csproj";
                processReference.StartInfo.WorkingDirectory = rootPath;
                processReference.Start();

                //Change WebApp Project Property To Library Output, instead of the Console Application
                var projectPath = $"{rootPath}\\{moduleName}\\{moduleName}.csproj";
                var xml = XDocument.Load(projectPath);
                var ns = xml.Root.Name.Namespace;
                var targetFramework = xml.Descendants(ns + "TargetFramework").FirstOrDefault();
                var propertyGroup = xml.Descendants(ns + "PropertyGroup").First();
                if (targetFramework != null)
                {
                    targetFramework.Value = "net8.0";
                    propertyGroup.Add(new XElement(ns + "OutputType", "Library"));
                }
                else
                {                   
                    propertyGroup.Add(new XElement(ns + "OutputType", "Library"));
                    propertyGroup.Add(new XElement(ns + "TargetFramework", "net8.0"));
                    
                }
                xml.Save(projectPath);

            }
            else
            {
                Console.WriteLine($"\n !ALERT MESSAGE: This module already exist --->{moduleName}, you cant create");
            }
            #endregion Module Name
        }

        static void creaeteEntities(string moduleName, string[] entities, string rootPath, bool? services, bool? caching)
        {
            
            bool moduleExist = verifyModule(moduleName, rootPath);
            //string rootPath = "D:\\imagina\\.net\\netcore\\CliPlatform"; FOR DEBUG...
            if (!moduleExist)
            {
                Console.WriteLine($"\n !ALERT MESSAGE: This module not exist --->{moduleName}");
            }
            else { 
            #region Entity Name
            int counterEntities = 0;

                //var notExistAnyEntity = verifyEntitiesNotExistAny(rootPath, entities, moduleName);
                var listEntitiesExist = verifyListEntitiesExist(rootPath, entities, moduleName);
                if (!listEntitiesExist.IsNullOrEmpty())
                {
                    string listEntities = String.Join(", ", listEntitiesExist);
                    Console.WriteLine($"\n !ALERT MESSAGE: That entities cant be created because already exist: {listEntities}");
                    //Console.WriteLine($"\n !ALERT MESSAGE: That entities cant be created because already exist");
                }                
                else{
                    //can be create all entities
                    foreach (string entity in entities)
                    if (!string.IsNullOrEmpty(entity))
                    {
                        // Capitalize  and pluralize Entities and Module
                        moduleName = StringHelper.Capitalize(moduleName.Trim());
                        string entityName = StringHelper.Capitalize(entity.Trim());

                        var lowerpluralizeentityName = StringHelper.Lowercase(entityName.Trim()).Pluralize();
                        var upperpluralizeentityName = StringHelper.Capitalize(entityName.Trim()).Pluralize();
                        var kebabCase = ConvertToKebabCase(entityName);
                        var lowermoduleName = StringHelper.Lowercase(moduleName.Trim());


                        #region Creating Controllers
                        //If exist! Creating Controllers Folder
                        var controllerRootModule = rootPath + "\\" + moduleName;
                        Console.WriteLine($"Root Module to Controllers: {controllerRootModule}");
                        var controllerFolderPath = Path.Combine(controllerRootModule, "Controllers");
                        bool existController = Directory.Exists(controllerFolderPath);
                        if (!existController)
                        {
                            Directory.CreateDirectory(controllerFolderPath);
                        }
                        //Creating Controller into controllers Folder
                        var controllersRootRepository = rootPath + "\\" + moduleName + "\\" + "Controllers";
                        Console.WriteLine($"Root Module to Controllers: {controllersRootRepository}");
                        var controllerResource = "CliPlatform.Templates.BasicEntities.Controller.Controller.txt";
                        creatingEntityFromTxtFile(controllerResource, entityName + "Controller.cs", moduleName, entityName, controllersRootRepository, lowermoduleName, lowerpluralizeentityName, upperpluralizeentityName, kebabCase.Pluralize());
                        #endregion


                        #region Creating Repositories
                        //If exist! Creating Repository Folder
                        var repositoriesRootModule = rootPath + "\\" + moduleName;
                        Console.WriteLine($"Root Module to Repositories: {repositoriesRootModule}");
                        var repositoriesFolderPath = Path.Combine(repositoriesRootModule, "Repositories");
                        bool existRepository = Directory.Exists(repositoriesFolderPath);
                        if (!existRepository)
                        {
                            Directory.CreateDirectory(repositoriesFolderPath);
                        }

                        //Creating Repository into Repositories Folder
                        var repositoriesRoot = rootPath + "\\" + moduleName + "\\" + "Repositories";
                        Console.WriteLine($"Root Module to Repositories: {repositoriesRoot}");
                        var repositoryResource = "CliPlatform.Templates.BasicEntities.Repositories.Repository.txt";
                        creatingEntityFromTxtFile(repositoryResource, entityName + "Repository.cs", moduleName, entityName, repositoriesRoot, lowermoduleName, lowerpluralizeentityName);


                        //If exist! Creating Interface Repository Folder
                        var irepositoriesRootModule = rootPath + "\\" + moduleName + "\\" + "Repositories";
                        Console.WriteLine($"Root Module to Repositories: {irepositoriesRootModule}");
                        var irepositoriesFolderPath = Path.Combine(irepositoriesRootModule, "Interfaces");
                        bool existIRepository = Directory.Exists(irepositoriesFolderPath);
                        if (!existIRepository)
                        {
                            Directory.CreateDirectory(irepositoriesFolderPath);
                        }

                        //Creating Repository Interface into Repositories Folder
                        var repositoryInterfaceResource = "CliPlatform.Templates.BasicEntities.Repositories.IRepository.txt";
                        creatingEntityFromTxtFile(repositoryInterfaceResource, "I" + entityName + "Repository.cs", moduleName, entityName, irepositoriesFolderPath, lowermoduleName, lowerpluralizeentityName);
                            #endregion

                            if (services == true)
                            {

                                #region Creating Services            
                                var servicesRootModule = rootPath + "\\" + moduleName;
                                Console.WriteLine($"Root Module to Services: {servicesRootModule}");
                                var servicesFolderPath = Path.Combine(servicesRootModule, "Services");
                                bool existService = Directory.Exists(servicesFolderPath);
                                //If exist! Creating Services Folder
                                if (!existService)
                                {
                                    Directory.CreateDirectory(servicesFolderPath);
                                }

                                //Creating Service into Services Folder
                                var servicesRoot = rootPath + "\\" + moduleName + "\\" + "Services";
                                Console.WriteLine($"Root Module to Services: {servicesRoot}");
                                var serviceResource = "CliPlatform.Templates.BasicEntities.Services.Service.txt";
                                creatingEntityFromTxtFile(serviceResource, entityName + "Service.cs", moduleName, entityName, servicesRoot, lowermoduleName, lowerpluralizeentityName);

                                var iservicesRootModule = rootPath + "\\" + moduleName + "\\" + "Services";
                                Console.WriteLine($"Root Module to Services: {iservicesRootModule}");
                                var iservicesFolderPath = Path.Combine(iservicesRootModule, "Interfaces");
                                bool existIService = Directory.Exists(iservicesFolderPath);
                                //If exist! Creating Interface Service Folder
                                if (!existIService)
                                {
                                    Directory.CreateDirectory(iservicesFolderPath);
                                }

                                //Creating Repository Interface into Services Folder
                                var serviceInterfaceResource = "CliPlatform.Templates.BasicEntities.Services.IService.txt";
                                creatingEntityFromTxtFile(serviceInterfaceResource, "I" + entityName + "Service.cs", moduleName, entityName, iservicesFolderPath, lowermoduleName, lowerpluralizeentityName);
                                #endregion
                            }



                            if (caching == true)
                            {

                                #region Creating Services            
                                var CachingRootModule = rootPath + "\\" + moduleName + "\\" + "Repositories";
                                Console.WriteLine($"Root Module to Caching: {CachingRootModule}");
                                var CachingFolderPath = Path.Combine(CachingRootModule, "Caching");
                                bool existCaching = Directory.Exists(CachingFolderPath);
                                //If exist! Creating Caching Folder
                                if (!existCaching)
                                {
                                    Directory.CreateDirectory(CachingFolderPath);
                                }

                                //Creating Cache into Caching Folder
                                var cachingRoot = rootPath + "\\" + moduleName + "\\" + "Repositories" + "\\" + "Caching";
                                Console.WriteLine($"Root Module to Services: {cachingRoot}");
                                var CachingResource = "CliPlatform.Templates.BasicEntities.Caching.Caching.txt";
                                creatingEntityFromTxtFile(CachingResource, "Cached" + entityName + "Repository.cs", moduleName, entityName, cachingRoot, lowermoduleName, lowerpluralizeentityName);
                                #endregion
                            }


                        #region Edit File ModuleConfigurationExtension
                        var ConfigurationExtensionPath = rootPath + "\\" + moduleName + "\\" + moduleName + "ConfigurationExtension.cs";
                        var configurationExtensionContent = File.ReadAllText(ConfigurationExtensionPath);

                        // declare a nested dictionary with the comments, the lines and the spaces to can see better into file
                        var linesAppend = new Dictionary<string, Dictionary<string, string>>();
                        linesAppend.Add("//appendUsing", new Dictionary<string, string>());
                        linesAppend.Add("//appendRepositories", new Dictionary<string, string>());
                        linesAppend.Add("//appendServices", new Dictionary<string, string>());
                        linesAppend.Add("//appendDecorators", new Dictionary<string, string>());

                        // add the namespaces to the using comment
                        linesAppend["//appendUsing"].Add($"using {moduleName}.Repositories;", "");
                        linesAppend["//appendUsing"].Add($"using {moduleName}.Repositories.Interfaces;", "");
                        if (services == true)
                           {
                            linesAppend["//appendUsing"].Add($"using {moduleName}.Services;", "");
                            linesAppend["//appendUsing"].Add($"using {moduleName}.Services.Interfaces;", "");
                           }

                        if (caching == true)
                        {
                            linesAppend["//appendUsing"].Add($"using {moduleName}.Repositories.Caching;", "");
                        }

                            // add the lines to the repositories and services comments with the indentation
                            linesAppend["//appendRepositories"].Add($"builder.Services.AddScoped<typeof(I{entityName}Repository), typeof({entityName}Repository)>();", "            ");
                        if (services == true)
                          {
                           linesAppend["//appendServices"].Add($"builder.Services.AddScoped<I{entityName}Service, {entityName}Service>();", "            ");
                          }

                        if (caching == true)
                        {
                            linesAppend["//appendDecorators"].Add($"builder.Services.Decorate<I{entityName}Repository, Cached{entityName}Repository>();", "            ");
                        }


                            foreach (var outerPair in linesAppend)
                        {
                            foreach (var innerPair in outerPair.Value)
                            {
                                // check if line is not in the file content
                                if (!configurationExtensionContent.Contains(innerPair.Key))
                                {
                                    // add the line with the comment and the space
                                    configurationExtensionContent = EditFile(configurationExtensionContent, outerPair.Key, $"\n{innerPair.Value}{innerPair.Key}");

                                    // read new content file
                                    File.WriteAllText(ConfigurationExtensionPath, configurationExtensionContent);
                                }
                            }
                        }
                        #endregion


                        #region Creating Entity
                        var dataRootRepository = rootPath + "\\Idata\\Entities";
                        Console.WriteLine($"Root Data: {dataRootRepository}");

                        // Get the root path into Idata/Entities
                        var modulePath = Path.Combine(dataRootRepository, moduleName);
                        Directory.CreateDirectory(modulePath);
                        Console.WriteLine($"Created folder {modulePath}");

                        //Creating Entity into Idata/Entities
                        var dataResource = "CliPlatform.Templates.BasicEntities.DataEntities.Entity.txt";
                        creatingEntityFromTxtFile(dataResource, entityName + ".cs", moduleName, entityName, modulePath, lowermoduleName, lowerpluralizeentityName, upperpluralizeentityName);
                        #endregion


                        #region Editing Context Files
                        // declare a dictionary with the comments and the lines
                        var linesContext = new Dictionary<string, string>();
                        linesContext.Add("//appendConsoleLineEntity", $"        public virtual DbSet<Idata.Entities.{moduleName}.{entityName}> {entityName.Pluralize()} {{ get; set; }} = null!;");
                        linesContext.Add("//appendUsingCommandLine", $"using Idata.Entities.{moduleName};");
                        // declare an array with the file names
                        var fileNames = new string[] { @"\Idata\Data\IdataContext.cs", @"\Platform\Data\PlatformContext.cs" };

                        foreach (var fileName in fileNames)
                        {
                            // change the value of filePath and fileContent
                            var filePath = rootPath + fileName;
                            var fileContent = File.ReadAllText(filePath);

                            foreach (var pair in linesContext)
                            {
                                // check if line is not in the file content
                                if (!fileContent.Contains(pair.Value))
                                {
                                    // add the line with the comment and the indentation
                                    fileContent = EditFile(fileContent, pair.Key, $"\n{pair.Value}");

                                    // read new content file
                                    File.WriteAllText(filePath, fileContent);
                                }
                            }
                        }
                        #endregion


                        #region Editing Program.cs into platform

                        // declare a dictionary with the comments and the lines
                        var linesPlatform = new Dictionary<string, string>();
                        linesPlatform.Add("//appendBuilder", $"builder = {moduleName}.{moduleName}ServiceProvider.Boot(builder);");
                        linesPlatform.Add("//appendSeeder", $"    {moduleName}Seeder.Seed(app);");
                        linesPlatform.Add("//appendUsingSeeder", $"using {moduleName}.Data.Seeders;");

                        foreach (var fileName in fileNames)
                        {
                            // change the value of filePath and fileContent
                            var programFilePath = rootPath + @"\Platform\Program.cs";
                            var programContent = File.ReadAllText(programFilePath);

                            if (!programContent.Contains($"\nbuilder = {moduleName}.{moduleName}ServiceProvider.Boot(builder);"))
                            {
                                // add the using line
                                programContent = EditFile(programContent, "//appendBuilder", $"\nbuilder = {moduleName}.{moduleName}ServiceProvider.Boot(builder);");

                                //Add project references
                                var processReference = new System.Diagnostics.Process();
                                processReference.StartInfo.FileName = "dotnet";
                                processReference.StartInfo.Arguments = $"add {rootPath}\\Platform\\Platform.csproj reference " +
                                                                        $"{rootPath}\\{moduleName}\\{moduleName}.csproj ";
                                processReference.StartInfo.WorkingDirectory = rootPath;
                                processReference.Start();
                            }


                            foreach (var pair in linesPlatform)
                            {
                                // check if line is not in the file content
                                if (!programContent.Contains(pair.Value))
                                {
                                    // add the line with the comment and the indentation
                                    programContent = EditFile(programContent, pair.Key, $"\n{pair.Value}");

                                    // read new content file
                                    File.WriteAllText(programFilePath, programContent);
                                }
                            }
                        }
                        #endregion


                        #region Permissions
                        // read content file permissions.cs
                        var permissionsPath = rootPath + "\\" + moduleName + "\\" + "Config" + "\\" + "Permissions.cs";
                        var permissionsContent = File.ReadAllText(permissionsPath);
                        string oldText = "}\n//append";
                        string newText = "},\n//append\n        }\";\n    }\n}";
                        int lastIndex = permissionsContent.TrimEnd().LastIndexOf(oldText);
                        if (lastIndex >= 0)
                        {
                            permissionsContent = permissionsContent.Substring(0, lastIndex) + newText;
                            File.WriteAllText(permissionsPath, permissionsContent);
                        }

                        // declare the string to append
                        var appendString = $@"'{moduleName.ToLower()}.{ConvertToKebabCase(entityName).Pluralize()}': {{
                'manage': '{moduleName.ToLower()}::{ConvertToKebabCase(entityName).Pluralize()}.manage',
                'index': '{moduleName.ToLower()}::{ConvertToKebabCase(entityName).Pluralize()}.list resource',
                'edit': '{moduleName.ToLower()}::{ConvertToKebabCase(entityName).Pluralize()}.edit resource',
                'create': '{moduleName.ToLower()}::{ConvertToKebabCase(entityName).Pluralize()}.create resource',
                'destroy': '{moduleName.ToLower()}::{ConvertToKebabCase(entityName).Pluralize()}.destroy resource',
                'restore': '{moduleName.ToLower()}::{ConvertToKebabCase(entityName).Pluralize()}.restore resource'
                }}";

                        // increment the counter by one
                        counterEntities++;

                        // if it is not the last entity, add a comma
                        if (counterEntities != entities.Length)
                        {
                            appendString += ",";
                        }

                        // add the comment //append to the end of the string
                        appendString += "\n//append";

                        // find the position to insert the string
                        var position = permissionsContent.LastIndexOf("//append");

                        // remove the original comment //append
                        permissionsContent = permissionsContent.Remove(position, "//append".Length);

                        // insert the string at the position
                        permissionsContent = permissionsContent.Insert(position, appendString);

                        // read new content file
                        File.WriteAllText(permissionsPath, permissionsContent);
                        #endregion
                    }
                }
            }
            #endregion Entity Name
        }

        static void DeleteFile(string fileName, string dataRootModule)
        {
            Console.WriteLine($"Root Module to File {fileName}: {dataRootModule}");
            var filePath = Path.Combine(dataRootModule, fileName);
            bool exists = File.Exists(filePath);
            while (!exists)
            {
                Thread.Sleep(1000);
                exists = File.Exists(filePath);
            }
            try
            {
                File.Delete(filePath);
            }
            catch (IOException ex)
            {
                Console.WriteLine("Error to delete File: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("You no has permission to delete File: " + ex.Message);
            }
        }


        static void DeleteFolder(string folderName, string dataRootModule)
        {
            Console.WriteLine($"Root Module to Folder {folderName}: {dataRootModule}");
            var pagesFolderPath = Path.Combine(dataRootModule, folderName);
            bool exists = Directory.Exists(pagesFolderPath);
            while (!exists)
            {
                Thread.Sleep(1000);
                exists = Directory.Exists(pagesFolderPath);
            }
            try
            {
                Directory.Delete(pagesFolderPath, true);
            }
            catch (IOException ex)
            {
                Console.WriteLine("Error to delete Folder: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("You no has permission to delete Folder: " + ex.Message);
            }
        }

        static void creatingModuleFromTxtFile(string resourceName, string file, string moduleName, string configFolderPath, string lowerModuleName = null)
        {
            //Read the template file and replace the placeholder with the entity name
            var assembly = Assembly.GetExecutingAssembly();
            //var resourcesPath = assembly.GetManifestResourceNames();
            Console.WriteLine("  " + resourceName);
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var template = reader.ReadToEnd();
                var content = string.Format(template, moduleName, lowerModuleName);

                //Write the content to the new file
                var classCmsPagesPath2 = Path.Combine(configFolderPath, file);
                File.WriteAllText(classCmsPagesPath2, content);
            }
        }

        static void creatingEntityFromTxtFile(string resourceName, string file, string moduleName, string entityName, string configFolderPath, string lowermoduleName = null, string lowerpluralizeentityName = null, string upperpluralizeentityName = null, string kebabCase = null)
        {
            //Read the template file and replace the placeholder with the entity name
            var assembly = Assembly.GetExecutingAssembly();
            //var resourcesPath = assembly.GetManifestResourceNames();
            Console.WriteLine("  " + resourceName);
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                //moduleName = {0}
                //entityName = {1}
                //lowermoduleName = {2}
                //lowerpluralizeentityName = {3}
                //upperpluralizeentityName = {4}
                //kebabCase = {5}
                var template = reader.ReadToEnd();
                var content = string.Format(template, moduleName, entityName, lowermoduleName, lowerpluralizeentityName, upperpluralizeentityName, kebabCase);

                //Write the content to the new file
                var classCmsPagesPath2 = Path.Combine(configFolderPath, file);
                File.WriteAllText(classCmsPagesPath2, content);
            }
        }

        static string EditFile(string fileContent, string comment, string newLines)
        {
            // find comment
            var index = fileContent.IndexOf(comment);
            // insert lines into file
            fileContent = fileContent.Insert(index + comment.Length, newLines);
            // return new content
            return fileContent;
        }

        static bool verifyModule(string moduleName, string rootPath)
        {
            var rootModule = rootPath + "\\" + moduleName;
            //Console.WriteLine($"Path Module: {rootModule}");
            bool exists = Directory.Exists(rootModule);
            //Console.WriteLine($"Verify if exist module {moduleName}: {rootModule}---> exist? {exists}");
            return exists;
        }


        static bool verifyEntitiesNotExistAny(string rootPath, string[] entities, string moduleName)
        {
            bool notExistAny = true;
            foreach (string entity in entities)
            {
                //Console.WriteLine($"RootPath entity: {rootPath}");
                string path = Path.Combine(rootPath, $"Idata\\Entities\\{moduleName}\\", entity + ".cs");
                //Console.WriteLine($"Path entity: {path}");
                bool exist = File.Exists(path);
                //Console.WriteLine($"Verify if exist Entity {entity} : exist? ---> {exist}");
                if (exist) notExistAny = false;
            }
            return notExistAny;
        }



        static string[] verifyListEntitiesExist(string rootPath, string[] entities, string moduleName)
        {
            List<string> entidadesExistentes = new List<string>();
            foreach (string entity in entities)
            {
                Console.WriteLine($"RootPath entity: {rootPath}");
                string path = Path.Combine(rootPath, $"Idata\\Entities\\{moduleName}\\", entity + ".cs");
                Console.WriteLine($"Path entity: {path}");
                bool exist = File.Exists(path);
                Console.WriteLine($"Verify if exist Entity {entity} : exist? ---> {exist}");
                if (exist) entidadesExistentes.Add(entity);
            }
            return entidadesExistentes.ToArray();
        }


        static string ConvertToKebabCase(string input)
        {
            var sb = new StringBuilder();
            sb.Append(char.ToLower(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    sb.Append('-');
                    sb.Append(char.ToLower(input[i]));
                }
                else
                {
                    sb.Append(input[i]);
                }
            }

            return sb.ToString();
        }


        static bool ConvertToBool(string input)
        {
            bool result;
            if (!bool.TryParse(input, out result))
            {
                result = true;
            }
            return result;
        }

        static void creaeteEntityDocumentation(string moduleName, string[] entities, string rootPath)
        {
            bool moduleExist = verifyModule(moduleName, rootPath);
            //string rootPath = "D:\\imagina\\.net\\netcore\\CliPlatform"; FOR DEBUG...
            if (!moduleExist)
            {
                Console.WriteLine($"\n !ALERT MESSAGE: This module not exist --->{moduleName}");
            }
            else
            {
                Console.WriteLine(rootPath);
                Console.WriteLine($"\n Mododule Exist --->{moduleName}");

                string[] files = Directory.GetFiles($"{rootPath}\\Idata\\Entities\\{moduleName}\\" , "*.cs");
                //Console.WriteLine(string.Join(", ", files));

                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    //Console.WriteLine(file);

                    string fileContent = File.ReadAllText(file);
                    //Console.WriteLine(fileContent);

                    Match match = Regex.Match(fileContent, @"\[Table\(""(?<table>[^""]+)"", Schema = ""(?<schema>[^""]+)""\)\]");

                    if (match.Success)
                    {
                        Console.WriteLine("1- Match Table and Schema");
                        string tableName = match.Groups["table"].Value;
                        string schemaName = match.Groups["schema"].Value;

                        Match entityMatch = Regex.Match(fileContent, @"public class (?<entity>[^{]+)");
                        Match entityPartialMatch = Regex.Match(fileContent, @"public partial class (?<entity>[^{]+)");

                        if (entityMatch.Success || entityPartialMatch.Success)
                        {
                            Console.WriteLine("2- Class Name Match");
                            string entityName = entityMatch.Success ? entityMatch.Groups["entity"].Value.Trim() : entityPartialMatch.Groups["entity"].Value.Trim();
                            Console.WriteLine($"Schema and Table: {schemaName}.{tableName}");
                            Console.WriteLine($"Entity: {entityName}");

                            int colonIndex = entityName.IndexOf(':');
                            string simpleEntity = entityName.Substring(0, colonIndex).Trim();
                            Console.WriteLine($"Entity Simple: {simpleEntity}");

                            MatchCollection propertyMatches = Regex.Matches(fileContent, @"public\s+(?<type>[^\s]+)\s+(?<property>[^\s]+)\s*\{.*?\}");
                            //Console.WriteLine("Properties:");
                            //foreach (Match propertyMatch in propertyMatches)
                            //{
                            //    Console.WriteLine($"- {propertyMatch.Groups["property"].Value}");
                            //}

                            MatchCollection relationalFieldMatches = Regex.Matches(fileContent, @"\[ForeignKey\(""(?<field>[^""]+)""\)\]\s*\[RelationalField\]\s");
                            //Console.WriteLine("Relational Fields:");
                            //foreach (Match fieldMatch in relationalFieldMatches)
                            //{
                            //    Console.WriteLine($"- {fieldMatch.Groups["field"].Value}");
                            //}

                            MatchCollection relationalManyToManyMatches = Regex.Matches(fileContent, @"public virtual List<(?<type>\w+)\>\??\s+(?<name>\w+)\s*\{.*?\}");
                           
                            List<string> properties = new List<string>();
                            List<string> relationalFields = new List<string>();
                            List<string> relationalManyToManyFields = new List<string>();

                            foreach (Match propertyMatch in propertyMatches)
                            {
                                string type = propertyMatch.Groups["type"].Value;
                                string property = propertyMatch.Groups["property"].Value;
                                if (type.EndsWith("?"))
                                {
                                    properties.Add($"{type.TrimEnd('?')}? {property}");
                                }
                                else
                                {
                                    properties.Add($"{type} {property}");
                                }
                            }

                            foreach (Match fieldMatch in relationalFieldMatches)
                            {
                                string fieldName = fieldMatch.Groups["field"].Value;
                                relationalFields.Add(fieldName);
                            }

                            foreach (Match fieldManyMatch in relationalManyToManyMatches)
                            {
                                string type = fieldManyMatch.Groups["type"].Value;
                                string name = fieldManyMatch.Groups["name"].Value;
                                string interrogation = fieldManyMatch.Groups["interrogation"].Value;
                                if (interrogation != "")
                                {
                                    relationalManyToManyFields.Add($"List<{type}>? {name}");
                                }
                                else
                                {
                                    relationalManyToManyFields.Add($"List<{type}> {name}");
                                }

                            }


                            string docContent = $"\n**Schema and Table:** {schemaName}.{tableName}\n" +
                                                $"\n**Entity:** {entityName}\n" +
                                                $"\n**Properties:**\n";
                            foreach (string property in properties)
                            {
                                docContent += $"- {property}\n";
                            }

                            docContent += $"\n**Relational Fields:**\n";
                            foreach (string field in relationalFields)
                            {
                                docContent += $"- {field}\n";
                            }

                            docContent += $"\n**Relational Many To Many:**\n";
                            foreach (string field in relationalManyToManyFields)
                            {                                
                                docContent += $"- {field}\n";
                            }


                            #region Permissions
                            //Console.WriteLine($"REGION Permissions!!!");
                            string filesPermission = $"{rootPath}\\{moduleName}\\Config\\Permissions.cs";
                            if (File.Exists(filesPermission))
                            {
                                Console.WriteLine($"\nFile Permissions Exist --->{filesPermission}");
                                // Leer el archivo
                                string permissions = File.ReadAllText(filesPermission);
                                Console.WriteLine($"Entity SuperSimple: {simpleEntity}");
                                Console.WriteLine("Entitt Name KebabCase: "+ConvertToKebabCase(simpleEntity).Pluralize());
                                string entity = $"{moduleName}.{ConvertToKebabCase(simpleEntity).Pluralize()}";
                                Console.WriteLine($"Entity to Search: {entity}");
                                string pattern = $@"'{entity}'\s*:\s*{{([^}}]+)}}";
                                Console.WriteLine($"Pattern: {pattern}");
                                Match matchPermissions = Regex.Match(permissions, pattern);
                                if (matchPermissions.Success)
                                {
                                    docContent += $"\n**Permissions:** {entity}";

                                    string input = matchPermissions.Groups[1].Value;
                                    string[] lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                                    string result = string.Join(Environment.NewLine, lines.Select(line => line.TrimStart()));
                                    docContent += result;

                                    Console.WriteLine("Match found!!");
                                }
                                else
                                {
                                    Console.WriteLine("The specified entity was not found.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Permissions file does not exist in path {filesPermission}");
                            }                       

                            #endregion

                            string docFileName = $"{fileName}.MD";
                            string docTxtFilePath = Path.Combine($"{rootPath}\\Idata\\Entities\\{moduleName}\\", "docu", docFileName);

                            Directory.CreateDirectory(Path.GetDirectoryName(docTxtFilePath));
                            File.WriteAllText(docTxtFilePath, docContent);
                        }
                    }
                }
            }
        }
    }
}
