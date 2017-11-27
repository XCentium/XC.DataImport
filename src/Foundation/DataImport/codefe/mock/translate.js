module.exports = {
  'GET /sitecore/api/ssc/sci/translate/8E159D2D-E688-4638-964D-E3B20DDC2247/gettranslations': function (req, res) {
    res.json([]);
  },
  'GET /sitecore/api/ssc/sci/translate/2218B78E-5A07-462F-A9D6-15E086742612/gettranslations': function (req, res) {
    res.json([
      { Phrase: 'Back', Key: 'BACK' },
      { Phrase: 'Log out', Key: 'LOG_OUT' }
    ]);
  }
};
