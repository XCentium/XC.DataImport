module.exports = {
    'GET /sitecore/api/ssc/dataimport/mappings/1e63916d-01f6-4b6e-a25c-d9e6fc244975/getbatchmapping': function (req, res) {
      res.json(
          {"data":{
              "Id": "1e63916d-01f6-4b6e-a25c-d9e6fc244975",
              "Name": "Test Batch",
              "Mappings": [
                 { "Id":"8e63916d-01f6-4b6e-a25c-d9e6fc244975", "Name": "test" }
              ]
          },"messages":""}
        );
    }
  };