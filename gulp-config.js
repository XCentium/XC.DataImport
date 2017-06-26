module.exports = function () {
    var instanceRoot = "C:\\Websites\\car-import";
    var config = {
        websiteRoot: instanceRoot + "\\Website",
        sitecoreLibraries: instanceRoot + "\\Website\\bin",
        licensePath: instanceRoot + "\\Data\\license.xml",
        solutionName: "XC.DataImport",
        buildConfiguration: "Debug",
        runCleanBuilds: false
    };
    return config;
}