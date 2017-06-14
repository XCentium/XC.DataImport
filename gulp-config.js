module.exports = function () {
    var instanceRoot = "C:\\inetpub\\wwwroot\\xc.dataimport";
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