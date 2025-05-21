using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class UnitRoomTranslator
    {
        public static ErrorCode TranslateEntityToBO(UnitRooms _entity, RoomBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.RoomId;
            bo.UnitId = _entity.UnitId;
            bo.Number = _entity.Number;
            bo.Surface = (decimal)_entity.Surface;
            bo.Type = (RoomType)_entity.Type;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(UnitRooms _entity, RoomBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.UnitId = bo.UnitId;
            _entity.Number = bo.Number;
            _entity.Surface = bo.Surface;
            _entity.Type = (int)bo.Type;
            return ErrorCode.Success;
        }
    }
}
