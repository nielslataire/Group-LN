using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class BadWeatherDayTranslator
    {
        public static ErrorCode TranslateEntityToBO(BadWeatherDays _entity, BadWeatherDayBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.WeatherStationId = _entity.WeatherstationId;
            bo.Type = _entity.Type;
            bo.BWDate = _entity.Date;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(BadWeatherDays _entity, BadWeatherDayBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.WeatherstationId = bo.WeatherStationId;
            _entity.Type = bo.Type;
            _entity.Date = bo.BWDate;

            return ErrorCode.Success;
        }
    }
}
