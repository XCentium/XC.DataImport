module.exports = {
    'GET /sitecore/api/ssc/dataimport/mappings/-/getdatabases': function (req, res) {
      res.json({"data":[
        {"Name":"Select Database","Value":null,"Id":null},
        {"Name":"core","Value":"core","Id":"core"},
        {"Name":"master","Value":"master","Id":"master"},
        {"Name":"web","Value":"web","Id":"web"}
      ],"messages":""});
    }
  };
  