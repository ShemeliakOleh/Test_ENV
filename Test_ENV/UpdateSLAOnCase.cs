using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace TestEnvProj
{
    public class UpdateSLAOnCase : IPlugin
    {
        private IPluginExecutionContext _context;
        private IOrganizationService _service;

        public void Execute(IServiceProvider serviceProvider)
        {
            _context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            _service = (IOrganizationService)serviceProvider.GetService(typeof(IOrganizationService));
            var target = _context.InputParameters["Target"] as Entity;
            if (target?.LogicalName == "incident" && _context?.MessageName == "Create")
            {
                var customerRef = target.GetAttributeValue<EntityReference>("customerid");
                var customer = _service.Retrieve(customerRef.LogicalName, customerRef.Id, new ColumnSet("test_companyid"));
                var companyRef = customer.GetAttributeValue<EntityReference>("test_companyid");
                var company = _service?.Retrieve(companyRef.LogicalName, companyRef.Id, new ColumnSet("slaid"));
                target["slaid"] = (company.GetAttributeValue<EntityReference>("slaid"));
            }
            else if (target?.LogicalName == "account" && _context?.MessageName == "Update" && target["slaid"] != null)
            {
                var incidentQuery = new QueryExpression("incident")
                {
                    ColumnSet = new ColumnSet("incidentid"),
                    LinkEntities =
            {
                new LinkEntity
                {
                    LinkFromEntityName = "incident",
                    LinkFromAttributeName = "customerid",
                    LinkToEntityName = "contact",
                    LinkToAttributeName = "contactid",
                    LinkCriteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("test_companyid", ConditionOperator.Equal, target.Id)
                        }
                    }
                }
            }
                };
                var incidents = _service?.RetrieveMultiple(incidentQuery).Entities;
                if (incidents != null && incidents.Count > 0)
                {
                    var requests = new OrganizationRequestCollection();
                    foreach (var incident in incidents)
                    {
                        var update = new Entity("incident")
                        {
                            Id = incident.Id,
                            ["slaid"] = target["slaid"]
                        };
                        requests.Add(new UpdateRequest { Target = update });
                    }
                    var multipleRequest = new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings
                        {
                            ContinueOnError = true,
                            ReturnResponses = false
                        },
                        Requests = requests
                    };
                    _service?.Execute(multipleRequest);
                }
            }
        }
    }
}