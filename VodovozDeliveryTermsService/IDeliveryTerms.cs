﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace VodovozDeliveryTermsService
{
    interface IDeliveryTerms
    {
        [OperationContract]
        [
            WebGet(
                UriTemplate = "/api/deliverypoint?latitude={latitude}&longitude={longitude}",
                ResponseFormat = WebMessageFormat.Json
            )
        ]
        string GetRulesByDistrict(decimal latitude, decimal longitude);

    }
}