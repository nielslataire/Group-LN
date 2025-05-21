using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class ActivityTranslator
    {
        public static ErrorCode TranslateEntityToBO(Activity _entity, ActivityBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.ID = _entity.ActivityId;
            bo.Name = _entity.Omschrijving;
            if (_entity.Group != null)
            {
                bo.Group.ID = _entity.Group.GroupId;
                bo.Group.Name = _entity.Group.Name;
                bo.Group.Lot = System.Convert.ToInt16(_entity.Group.Lot);
            }

            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(Activity _entity, ActivityBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Omschrijving = bo.Name;
            if (bo.Group != null)
                // If _entity.ActivityGroup Is Nothing Then
                // _entity.ActivityGroup = New ActivityGroup()
                // _entity.ActivityGroup.Lot = bo.Group.Lot
                // _entity.ActivityGroup.Name = bo.Group.Name
                // Else
                _entity.GroupId = bo.Group.ID;
            return ErrorCode.Success;
        }
    }
}
