﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using NzbDrone.Core.Repository;
using SubSonic.Repository;
using TvdbLib.Data;

namespace NzbDrone.Core.Controllers
{
    public class SeriesController : ISeriesController
    {
        private readonly IConfigController _config;
        private readonly IDiskController _diskController;
        private readonly ILog _logger;
        private readonly IRepository _sonioRepo;
        private readonly ITvDbController _tvDb;

        public SeriesController(ILog logger, IDiskController diskController, IConfigController configController, IRepository dataRepository, ITvDbController tvDbController)
        {
            _logger = logger;
            _diskController = diskController;
            _config = configController;
            _sonioRepo = dataRepository;
            _tvDb = tvDbController;
        }

        #region ISeriesController Members

        public IQueryable<Series> GetSeries()
        {
            return _sonioRepo.All<Series>();
        }

        public Series GetSeries(int tvdbId)
        {
            return _sonioRepo.Single<Series>(s=> s.TvdbId == tvdbId);
        }



        public void SyncSeriesWithDisk()
        {

            foreach (string seriesFolder in _diskController.GetDirectories(_config.SeriesRoot))
            {
                var cleanPath  =_diskController.CleanPath(new DirectoryInfo(seriesFolder).FullName);
                if (!_sonioRepo.Exists<Series>(s => s.Path == cleanPath))
                {
                    _logger.InfoFormat("Folder '{0} isn't mapped to a series in the database. Trying to map it.'", cleanPath);
                    AddShow(cleanPath);
                }
            }

        }

        #endregion

        private void AddShow(string path)
        {
            List<TvdbSearchResult> searchResults = _tvDb.SearchSeries(new DirectoryInfo(path).Name);
            if (searchResults.Count != 0)
            {
                AddShow(path, _tvDb.GetSeries(searchResults[0].Id, searchResults[0].Language));
            }
        }

        private void AddShow(string path, TvdbSeries series)
        {
            _sonioRepo.Add(new Series { TvdbId = series.Id, SeriesName = series.SeriesName, AirTimes = series.AirsTime, AirsDayOfWeek = series.AirsDayOfWeek, Overview = series.Overview, Status = series.Status, Language = series.Language.Abbriviation, Path = path });
        }
    }
}