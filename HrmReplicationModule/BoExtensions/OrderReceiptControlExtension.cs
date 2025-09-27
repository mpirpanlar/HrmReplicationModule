using Sentez.Common.SystemServices;
using Sentez.Data.BusinessObjects;
using Sentez.Data.Tools;
using Sentez.Localization;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace Sentez.HrmReplicationModule.BoExtensions
{
    public class OrderReceiptControlExtension : BoExtensionBase
    {
        public OrderReceiptControlExtension(BusinessObjectBase bo)
            : base(bo)
        {
        }

        protected override void SetBusinessObject(BusinessObjectBase businessObject)
        {
            base.SetBusinessObject(businessObject);
            if (BusinessObject == null)
                return;
        }

        protected override void OnColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            base.OnColumnChanged(sender, e);
            if (!Enabled || _suppressEvents)
                return;
        }

        protected override void OnBeforePost(object sender, CancelEventArgs e)
        {
            if (!Enabled || _suppressEvents)
                return;
            base.OnBeforePost(sender, e);
        }
    }
}
