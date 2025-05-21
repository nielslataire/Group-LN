using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class WheaterstationTranslator
    {
        public static ErrorCode TranslateEntityToBO(WheaterStations _entity, WheaterStationBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            bo.Visible = _entity.Visible;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(WheaterStations _entity, WheaterStationBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            _entity.Visible = bo.Visible;
            return ErrorCode.Success;
        }
    }
}
