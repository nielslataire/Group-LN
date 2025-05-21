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
    public class UnitTypeTranslator
    {
        public static ErrorCode TranslateEntityToBO(UnitTypes _entity, UnitTypeBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            bo.Shortcode = _entity.Shortcode;
            bo.GroupId = _entity.GroupId;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(UnitTypes _entity, UnitTypeBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            _entity.Shortcode = bo.Shortcode;
            if ((bo.GroupId != 0))
                _entity.GroupId = bo.GroupId;
            return ErrorCode.Success;
        }
    }
}
