﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.OData;
using SenseNet.Search;
using SenseNet.Tests.Accessors;
using Task = System.Threading.Tasks.Task;
// ReSharper disable StringLiteralTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataOperationTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_OP_InvokeAction()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    var expectedJson = @"
                        {
                          ""d"": {
                            ""message"":""Action3 executed""
                          }
                        }";

                    // ACTION
                    var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action3",
                        "",
                        null).ConfigureAwait(false);

                    // ASSERT
                    var jsonText = response.Result;
                    var raw = jsonText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                    var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                    Assert.IsTrue(raw == exp);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeAction_NoContent()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action4",
                        "",
                        null).ConfigureAwait(false);

                    // ASSERT
                    Assert.IsTrue(response.StatusCode == 204);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeAction_Post_GetPutMergePatchDelete()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION POST
                    var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataAction",
                        "",
                        null).ConfigureAwait(false);

                    // ASSERT: POST Ok
                    Assert.AreEqual("ODataAction executed.", response.Result);

                    var verbs = new[] {"GET", "PUT", "MERGE", "PATCH", "DELETE"};
                    foreach (var verb in verbs)
                    {
                        // ACTION: GET PUT MERGE PATCH DELETE: error
                        response = await ODataCallAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataAction",
                            "",
                            null,
                            verb).ConfigureAwait(false);

                        // ASSERT: error
                        var error = GetError(response);
                        Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
                    }
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeFunction_PostGet_PutMergePatchDelete()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION: POST
                    var response = await ODataPostAsync(
                        "/OData.svc/Root('IMS')/ODataFunction",
                        "",
                        null).ConfigureAwait(false);

                    // ASSERT: POST ok
                    Assert.AreEqual("ODataFunction executed.", response.Result);

                    // ACTION: GET
                    response = await ODataGetAsync(
                        "/OData.svc/Root('IMS')/ODataFunction",
                        "")
                        .ConfigureAwait(false);

                    // ASSERT: GET ok
                    Assert.AreEqual("ODataFunction executed.", response.Result);

                    //------------------------------------------------------------ GET PUT MERGE PATCH DELETE: error
                    var verbs = new[] {"PUT", "MERGE", "PATCH", "DELETE"};
                    foreach (var verb in verbs)
                    {
                        // ACTION: PUT MERGE PATCH DELETE
                        response = await ODataCallAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataAction",
                            "",
                            null,
                            verb).ConfigureAwait(false);

                        // ASSERT: error
                        var error = GetError(response);
                        Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
                    }
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeFunction_DictionaryHandler()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                        "/OData.svc/Root/System/Schema/ContentTypes/GenericContent('FieldSettingContent')/ODataGetParentChainAction",
                        "?metadata=no&$select=Id,Name&$top=2&$inlinecount=allpages",
                        null)
                        .ConfigureAwait(false);

                    // ASSERT: POST ok
                    var entities = GetEntities(response);
                    Assert.AreEqual(6, entities.TotalCount);
                    Assert.AreEqual(2, entities.Length);
                }
            }).ConfigureAwait(false);
        }

        #region /* ===================================================================== ACTION RESOLVER */

        private class ActionResolverSwindler : IDisposable
        {
            private readonly IActionResolver _original;
            public ActionResolverSwindler(IActionResolver actionResolver)
            {
                _original = ODataMiddleware.ActionResolver;
                ODataMiddleware.ActionResolver = actionResolver;
            }

            public void Dispose()
            {
                ODataMiddleware.ActionResolver = _original;
            }
        }

        internal class TestActionResolver : IActionResolver
        {
            internal class Action1 : ActionBase
            {
                public override string Icon { get => "ActionIcon1"; set { } }
                public override string Name { get => "Action1"; set { } }
                public override string Uri => "ActionIcon1_URI";
                public override bool IsHtmlOperation => true;
                public override bool IsODataOperation => false;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action1 executed" } } } };
                }
            }
            internal class Action2 : ActionBase
            {
                public override string Icon { get => "ActionIcon2"; set { } }
                public override string Name { get => "Action2"; set { } }
                public override string Uri => "ActionIcon2_URI";
                public override bool IsHtmlOperation => true;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action2 executed" } } } };
                }
            }
            internal class Action3 : ActionBase
            {
                public override string Icon { get => "ActionIcon3"; set { } }
                public override string Name { get => "Action3"; set { } }
                public override string Uri => "ActionIcon3_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action3 executed" } } } };
                }
            }
            internal class Action4 : ActionBase
            {
                public override string Icon { get => "ActionIcon4"; set { } }
                public override string Name { get => "Action4"; set { } }
                public override string Uri => "ActionIcon4_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return null;
                }
            }

            internal class ChildrenDefinitionFilteringTestAction : ActionBase
            {
                public override string Icon { get => "ChildrenDefinitionFilteringTestAction"; set { } }
                public override string Name { get => "ChildrenDefinitionFilteringTestAction"; set { } }
                public override string Uri => "ChildrenDefinitionFilteringTestAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return new ChildrenDefinition
                    {
                        ContentQuery = "InFolder:/Root/IMS/BuiltIn/Portal",
                        EnableAutofilters = FilterStatus.Disabled,
                        PathUsage = PathUsageMode.NotUsed,
                        Sort = new[] { new SortInfo("Name", true) },
                        Skip = 2,
                        Top = 3
                    };
                }
            }
            internal class CollectionFilteringTestAction : ActionBase
            {
                public override string Icon { get => "ActionIcon4"; set { } }
                public override string Name { get => "Action4"; set { } }
                public override string Uri => "ActionIcon4_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF")
                        .Execute().Nodes.Select(Content.Create);
                }
            }

            internal class ODataActionAction : ActionBase
            {
                public override string Icon { get => "ODataActionAction"; set { } }
                public override string Name { get => "ODataActionAction"; set { } }
                public override string Uri => "ODataActionAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return "ODataAction executed.";
                }
            }
            internal class ODataFunctionAction : ActionBase
            {
                public override string Icon { get => "ODataFunctionAction"; set { } }
                public override string Name { get => "ODataFunctionAction"; set { } }
                public override string Uri => "ODataFunctionAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override object Execute(Content content, params object[] parameters)
                {
                    return "ODataFunction executed.";
                }
            }
            internal class ODataGetParentChainAction : ActionBase
            {
                public override string Icon { get => ""; set { } }
                public override string Name { get => "ODataGetParentChainAction"; set { } }
                public override string Uri => "ODataContentDictionaryFunctionAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override object Execute(Content content, params object[] parameters)
                {
                    var result = new List<Content>();
                    Content c = content;
                    while (true)
                    {
                        result.Add(c);
                        var n = c.ContentHandler.Parent;
                        if (n == null)
                            break;
                        c = Content.Create(n);
                    }
                    return result;
                }
            }

            public GenericScenario GetScenario(string name, string parameters, HttpContext httpContext)
            {
                return null;
            }
            public IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri, HttpContext httpContext)
            {
                return new ActionBase[] { new Action1(), new Action2(), new Action3(), new Action4() };
            }
            public ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters, HttpContext httpContext)
            {
                switch (actionName)
                {
                    default: return null;
                    case "Action1": return new Action1();
                    case "Action2": return new Action2();
                    case "Action3": return new Action3();
                    case "Action4": return new Action4();
                    //case "GetPermissions": return new GetPermissionsAction();
                    //case "SetPermissions": return new SenseNet.Portal.ApplicationModel.SetPermissionsAction();
                    //case "HasPermission": return new SenseNet.Portal.ApplicationModel.HasPermissionAction();
                    //case "AddAspects": return new SenseNet.ApplicationModel.AspectActions.AddAspectsAction();
                    //case "RemoveAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAspectsAction();
                    //case "RemoveAllAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAllAspectsAction();
                    //case "AddFields": return new SenseNet.ApplicationModel.AspectActions.AddFieldsAction();
                    //case "RemoveFields": return new SenseNet.ApplicationModel.AspectActions.RemoveFieldsAction();
                    //case "RemoveAllFields": return new SenseNet.ApplicationModel.AspectActions.RemoveAllFieldsAction();

                    case "ChildrenDefinitionFilteringTest": return new ChildrenDefinitionFilteringTestAction();
                    case "CollectionFilteringTest": return new CollectionFilteringTestAction();

                    case "ODataAction": return new ODataActionAction();
                    case "ODataFunction": return new ODataFunctionAction();

                    case "ODataGetParentChainAction": return new ODataGetParentChainAction();

                    //case "CopyTo": return new CopyToAction();
                    //case "MoveTo": return new MoveToAction();
                }
            }
        }
        /*
        ActionBase
            Action1
            Action2
            Action3
            Action4
            PortalAction
                ClientAction
                    OpenPickerAction
                        CopyToAction
                            CopyBatchAction
                        ContentLinkBatchAction
                        MoveToAction
                            MoveBatchAction
                    ShareAction
                    DeleteBatchAction
                        DeleteAction
                    WebdavOpenAction
                    WebdavBrowseAction
                UrlAction
                    SetAsDefaultViewAction
                    PurgeFromProxyAction
                    ExpenseClaimPublishAction
                    WorkflowsAction
                    OpenLinkAction
                    BinarySpecialAction
                    AbortWorkflowAction
                    UploadAction
                    ManageViewsAction
                    ContentTypeAction
                    SetNotificationAction
                ServiceAction
                    CopyAppLocalAction
                    LogoutAction
                    UserProfileAction
                    CopyViewLocalAction
                DeleteLocalAppAction
                ExploreAction
        */
        #endregion
    }
}