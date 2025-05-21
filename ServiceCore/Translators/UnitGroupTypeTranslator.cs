using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;
using DALCore;

namespace ServiceCore.Translators
{
    public class UnitGroupTypeTranslator
    {
        public static ErrorCode TranslateEntityToBO(UnitGroupTypes _entity, UnitGroupTypeBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            if ((_entity.UnitTypes != null))
            {
                foreach (var Type in _entity.UnitTypes)
                {
                    UnitTypeBO UnitType = new UnitTypeBO();
                    var err = UnitTypeTranslator.TranslateEntityToBO(Type, UnitType);
                    if ((err != ErrorCode.Success))
                        return err;
                    bo.UnitTypes.Add(UnitType);
                }
            }
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(UnitGroupTypes _entity, UnitGroupTypeBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            var err = HandleTypes(_entity, bo.UnitTypes);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }
        private static ErrorCode HandleTypes(UnitGroupTypes _entity, List<UnitTypeBO> types)
        {
            if ((types.Count == 0))
                return ErrorCode.Success;
            foreach (var x in types)
            {
                if ((x.Id == 0))
                {
                    // insert
                    UnitTypes type = new UnitTypes();
                    var err = UnitTypeTranslator.TranslateBOToEntity(type, x);
                    if ((err != ErrorCode.Success))
                        return err;
                    _entity.UnitTypes.Add(type);
                }
                else
                {
                    // update
                    var type = _entity.UnitTypes.FirstOrDefault(f => f.Id == x.Id);
                    if ((type != null))
                    {
                        var err = UnitTypeTranslator.TranslateBOToEntity(type, x);
                        if ((err != ErrorCode.Success))
                            return err;
                    }
                }
            }
            // delete
            List<UnitTypes> delList = new List<UnitTypes>();
            foreach (var x in _entity.UnitTypes)
            {
                if ((!types.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.UnitTypes.Remove(x);
            return ErrorCode.Success;
        }
    }
}
