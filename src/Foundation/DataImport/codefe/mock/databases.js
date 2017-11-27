module.exports = {
    'GET /sitecore/api/ssc/dataimport/mappings/-/getdatabases': function (req, res) {
      res.json({"data":[
        {"Name":"Select Database","ConnectionString":null,"Id":null},
        {"Name":"core","ConnectionString":"core","Id":"core"},
        {"Name":"master","ConnectionString":"master","Id":"master"},
        {"Name":"web","ConnectionString":"web","Id":"web"}
      ],"messages":""});
    }
  };
  