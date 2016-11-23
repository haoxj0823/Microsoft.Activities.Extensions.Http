// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SampleHttpServiceTest.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Tests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    using CannonicalWorkflowHttpWebApp.Models;

    using Microsoft.Activities.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WCFTestHelper;

    /// <summary>
    /// The Sample http service test.
    /// </summary>
    [TestClass]
    public class SampleHttpServiceTest
    {
        #region Constants and Fields

        /// <summary>
        ///   The localhost.
        /// </summary>
        public const string Localhost = "localhost";

        /// <summary>
        ///   Indicates which web server you want to use
        /// </summary>
        /// <remarks>
        ///   TODO: Enable PUT/DELETE with IIS Express 
        ///   Modify C:\Program Files (x86)\IIS Express\AppServer\applicationhost.config
        ///   Add the PUT / DELETE verbs to the following
        ///   <add name = "ExtensionlessUrl-Integrated-4.0" path = "*." verb = "GET,HEAD,POST,DEBUG,PUT,DELETE"
        ///     type = "System.Web.Handlers.TransferRequestHandler" preCondition = "integratedMode,runtimeVersionv4.0" />
        /// </remarks>
        public static readonly WebServer Server = WebServer.WebDevServer;

        // Using a different port for unit testing

        /// <summary>
        ///   The base uri format.
        /// </summary>
        private const string BaseUriFormat = "http://{0}:{1}/";

        /// <summary>
        ///   The json content type.
        /// </summary>
        private const string JsonContentType = "application/json";

        /// <summary>
        ///   The port.
        /// </summary>
        private const int Port = 5401;

        /// <summary>
        ///   The service path.
        /// </summary>
        private const string ServicePath = "api";

        /// <summary>
        ///   The xml content type.
        /// </summary>
        private const string XmlContentType = "application/xml";

        /// <summary>
        ///   host name for use with fiddler
        /// </summary>
        /// <remarks>
        ///   TODO: Enable fiddler for IIS Express
        ///   Modify Fiddler Rules to contain this 
        ///   static function OnBeforeRequest(oSession:Fiddler.Session)
        ///   {
        ///   //...
        ///   // workaround the iisexpress limitation
        ///   // URL http://iisexpress:port can be used for capturing IIS Express traffic
        ///   if (oSession.HostnameIs("iisexpress")) { oSession.host = "localhost:"+oSession.port; }
        ///   //...
        ///   }
        /// </remarks>
        private static readonly string FiddlerLocalhost;

        /// <summary>
        ///   Tip: Use this switch to control if you want to use fiddler for debugging
        /// </summary>
        private static readonly bool UseFiddler;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes static members of the <see cref = "SampleHttpServiceTest" /> class.
        /// </summary>
        static SampleHttpServiceTest()
        {
            FiddlerLocalhost = Server == WebServer.IISExpress ? "iisexpress" : "ipv4.fiddler";
            UseFiddler = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets URI for doing an AddOrUpdate with PUT
        /// </summary>
        public static string AddOrUpdateServiceUri
        {
            get
            {
                return ServiceUri + "/AddOrUpdate";
            }
        }

        /// <summary>
        ///   Gets BaseUri.
        /// </summary>
        public static string BaseUri
        {
            get
            {
                return string.Format(BaseUriFormat, UseFiddler ? FiddlerLocalhost : Localhost, Port);
            }
        }

        /// <summary>
        ///   Gets ServiceUri.
        /// </summary>
        public static string ServiceUri
        {
            get
            {
                return BaseUri + ServicePath;
            }
        }

        /// <summary>
        ///   Gets or sets the test context which provides
        ///   information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// The close servers.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// An unknown web server was requested
        /// </exception>
        [ClassCleanup]
        public static void CloseServers()
        {
            // TIP: Use helper classes to close servers required for testing
            // switch (Server)
            // {
            // case WebServer.WebDevServer:
            // WebDevServer40.Close(Port);
            // break;
            // case WebServer.IISExpress:
            // IISExpressServer.Close();
            // break;
            // default:
            // throw new ArgumentOutOfRangeException();
            // }

            // Could also close Fiddler
            // FiddlerDebugProxy.Close();
        }

        /// <summary>
        /// The start servers.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// An unknown web server was requested
        /// </exception>
        [ClassInitialize]
        public static void StartServers(TestContext context)
        {
            // switch (Server)
            // {
            // case WebServer.WebDevServer:
            // WebDevServer40.EnsureIsRunning(
            // Port, 
            // TestServerHelper.GetWebPathFromSolutionPath(context, @"Examples\CannonicalWorkflowHttpWebApp"));
            // break;
            // case WebServer.IISExpress:
            // IISExpressServer.EnsureIsRunning(
            // Port, 
            // TestServerHelper.GetWebPathFromSolutionPath(context, @"Examples\CannonicalWorkflowHttpWebApp"));
            // break;
            // default:
            // throw new ArgumentOutOfRangeException();
            // }

            // TIP: Fiddler is a great tool for understanding HTTP traffic http://www.fiddler2.com
            if (UseFiddler)
            {
                FiddlerDebugProxy.EnsureIsRunning();
            }
        }

        /// <summary>
        /// The delete should be idempotent.
        /// </summary>
        [TestMethod]
        public void DeleteShouldBeIdempotent()
        {
            // Arrange
            const int ResourceKey = 1;

            // Act
            var testResult1 = DeleteResource(ResourceKey);
            var testResult2 = DeleteResource(ResourceKey);

            // Assert
            testResult1.Response.EnsureSuccessStatusCode();
            testResult2.Response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// The delete should delete an entity that exists.
        /// </summary>
        [TestMethod]
        public void DeleteShouldDeleteAnEntityThatExists()
        {
            // Arrange
            const int ResourceKey = 1;

            // Act
            var testResult = DeleteResource(ResourceKey);
            testResult.Response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// The delete should return 400 bad request if the key is invalid.
        /// </summary>
        [TestMethod]
        public void DeleteShouldReturn400BadRequestIfTheKeyIsInvalid()
        {
            // Arrange
            const string ResourceKey = "badkey";

            // using key of type string to force bad request

            // Act
            using (var client = CreateHttpClient())
            {
                var deleteRequest = new HttpRequestMessage(
                    HttpMethod.Delete, string.Format("{0}/{1}", ServiceUri, ResourceKey));
                AddIfMatchHeader(deleteRequest, null);

                var response = client.Send(deleteRequest);
                var result = new TestResult(response);

                // Assert
                Assert.AreEqual(HttpStatusCode.BadRequest, result.Response.StatusCode);
            }
        }

        /// <summary>
        /// The delete should return with 412 precondition failed if no matching entity for if match etag.
        /// </summary>
        [TestMethod]
        public void DeleteShouldReturnWith412PreconditionFailedIfNoMatchingEntityForIfMatchEtag()
        {
            // Arrange
            const int ResourceKey = 1;

            // Act

            // Get the resource
            var resultGet = GetResource(ResourceKey);
            resultGet.Response.EnsureSuccessStatusCode();

            // Update so the etag won't match
            var putResource = resultGet.Sample;
            putResource.Data = "modified";

            var resultPut = PutResource(ResourceKey, putResource);
            resultPut.Response.EnsureSuccessStatusCode();

            // Try to delete - this should fail because of the precondition
            var result = DeleteResource(ResourceKey, resultGet.Sample.Tag);

            // Assert
            Assert.AreEqual(HttpStatusCode.PreconditionFailed, result.Response.StatusCode);
        }

        /// <summary>
        /// The delete should succeed if matching entity for if match etag.
        /// </summary>
        [TestMethod]
        public void DeleteShouldSucceedIfMatchingEntityForIfMatchEtag()
        {
            // Arrange
            const int ResourceKey = 1;

            // Act
            var resultGet = GetResource(ResourceKey);
            resultGet.Response.EnsureSuccessStatusCode();

            // Delete with etag
            var result = DeleteResource(ResourceKey, resultGet.Sample.Tag);
            var result2 = DeleteResource(ResourceKey, resultGet.Sample.Tag);

            // Assert
            result.Response.EnsureSuccessStatusCode();
            result2.Response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// The delete should succeed if wildcard used in if match etag.
        /// </summary>
        [TestMethod]
        public void DeleteShouldSucceedIfWildcardUsedInIfMatchEtag()
        {
            // Arrange
            const int ResourceKey = 1;

            // Act
            var resultGet = GetResource(ResourceKey);
            resultGet.Response.EnsureSuccessStatusCode();

            // Update so the etag won't match
            var putResource = resultGet.Sample;
            putResource.Data = "modified";
            PutResource(ResourceKey, putResource);

            // Delete with wildcard etag
            var result = DeleteResource(ResourceKey, "*");
            var result2 = DeleteResource(ResourceKey, resultGet.Sample.Tag);

            // Assert
            result.Response.EnsureSuccessStatusCode();
            result2.Response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// The get must return a resource given a key if the resource with that key exists json.
        /// </summary>
        [TestMethod]
        public void GetMustReturnAResourceGivenAKeyIfTheResourceWithThatKeyExistsJson()
        {
            GetMustReturnAResourceGivenAKeyIfTheResourceWithThatKeyExists(JsonContentType);
        }

        /// <summary>
        /// The get must return a resource given a key if the resource with that key exists xml.
        /// </summary>
        [TestMethod]
        public void GetMustReturnAResourceGivenAKeyIfTheResourceWithThatKeyExistsXml()
        {
            GetMustReturnAResourceGivenAKeyIfTheResourceWithThatKeyExists(XmlContentType);
        }

        /// <summary>
        /// The get must return resources starting with the first one when skip is not defined json.
        /// </summary>
        [TestMethod]
        public void GetMustReturnResourcesStartingWithTheFirstOneWhenSkipIsNotDefinedJson()
        {
            GetMustReturnResourcesStartingWithTheFirstOneWhenSkipIsNotDefined(JsonContentType);
        }

        /// <summary>
        /// The get must return resources starting with the first one when skip is not defined xml.
        /// </summary>
        [TestMethod]
        public void GetMustReturnResourcesStartingWithTheFirstOneWhenSkipIsNotDefinedXml()
        {
            GetMustReturnResourcesStartingWithTheFirstOneWhenSkipIsNotDefined(XmlContentType);
        }

        /// <summary>
        /// The get must return zero or more resources when take is not provided json.
        /// </summary>
        [TestMethod]
        public void GetMustReturnZeroOrMoreResourcesWhenTakeIsNotProvidedJson()
        {
            GetMustReturnZeroOrMoreResourcesWhenTakeIsNotProvided(JsonContentType);
        }

        /// <summary>
        /// The get must return zero or more resources when take is not provided xml.
        /// </summary>
        [TestMethod]
        public void GetMustReturnZeroOrMoreResourcesWhenTakeIsNotProvidedXml()
        {
            GetMustReturnZeroOrMoreResourcesWhenTakeIsNotProvided(XmlContentType);
        }

        /// <summary>
        /// The get must return zero resources when skip is greater than the number of resources in the collection json.
        /// </summary>
        [TestMethod]
        //// Test disabled until we can figure out how to support IQueryable
        [Ignore]
        public void GetMustReturnZeroResourcesWhenSkipIsGreaterThanTheNumberOfResourcesInTheCollectionJson()
        {
            GetMustReturnZeroResourcesWhenSkipIsGreaterThanTheNumberOfResourcesInTheCollection(JsonContentType);
        }

        /// <summary>
        /// The get must return zero resources when skip is greater than the number of resources in the collection xml.
        /// </summary>
        [TestMethod]
        //// Test disabled until we can figure out how to support IQueryable
        [Ignore]
        public void GetMustReturnZeroResourcesWhenSkipIsGreaterThanTheNumberOfResourcesInTheCollectionXml()
        {
            GetMustReturnZeroResourcesWhenSkipIsGreaterThanTheNumberOfResourcesInTheCollection(XmlContentType);
        }

        /// <summary>
        /// The get must skip skip resources in the collection and return up to take resources json.
        /// </summary>
        [TestMethod]
        //// Test disabled until we can figure out how to support IQueryable
        [Ignore]
        public void GetMustSkipSkipResourcesInTheCollectionAndReturnUpToTakeResourcesJson()
        {
            GetMustSkipSkipResourcesInTheCollectionAndReturnUpToTakeResources(JsonContentType);
        }

        /// <summary>
        /// The get must skip skip resources in the collection and return up to take resources xml.
        /// </summary>
        [TestMethod]
        //// Test disabled until we can figure out how to support IQueryable
        [Ignore]
        public void GetMustSkipSkipResourcesInTheCollectionAndReturnUpToTakeResourcesXml()
        {
            GetMustSkipSkipResourcesInTheCollectionAndReturnUpToTakeResources(XmlContentType);
        }

        /// <summary>
        /// The get should return an e tag header.
        /// </summary>
        [TestMethod]
        public void GetShouldReturnAnETagHeader()
        {
            // Arrange
            const int ExpectedKey = 1;

            // Act
            var result = GetResource(ExpectedKey);
            result.Response.EnsureSuccessStatusCode();

            // Assert
            Assert.IsNotNull(result.Response.Headers.ETag);
        }

        /// <summary>
        /// The initialize resource collection.
        /// </summary>
        [TestInitialize]
        public void InitializeResourceCollection()
        {
            using (var client = new HttpClient())
            {
                // TIP: Initialize your service to a known state before each test
                // Delete all records - service has special case code to do this
                using (var request = new HttpRequestMessage(HttpMethod.Delete, ServiceUri + "/all"))
                {
                    client.Send(request);
                }
            }
        }

        /// <summary>
        /// The post must append a valid resource to the resource collection json.
        /// </summary>
        [TestMethod]
        public void PostMustAppendAValidResourceToTheResourceCollectionJson()
        {
            PostMustAppendAValidResourceToTheResourceCollection(JsonContentType);
        }

        /// <summary>
        /// The post must append a valid resource to the resource collection xml.
        /// </summary>
        [TestMethod]
        public void PostMustAppendAValidResourceToTheResourceCollectionXml()
        {
            PostMustAppendAValidResourceToTheResourceCollection(XmlContentType);
        }

        /// <summary>
        /// The post must ignore writes to entity fields the server considers read only json.
        /// </summary>
        [TestMethod]
        public void PostMustIgnoreWritesToEntityFieldsTheServerConsidersReadOnlyJson()
        {
            PostMustIgnoreWritesToEntityFieldsTheServerConsidersReadOnly(JsonContentType);
        }

        /// <summary>
        /// The post must ignore writes to entity fields the server considers read only xml.
        /// </summary>
        [TestMethod]
        public void PostMustIgnoreWritesToEntityFieldsTheServerConsidersReadOnlyXml()
        {
            PostMustIgnoreWritesToEntityFieldsTheServerConsidersReadOnly(XmlContentType);
        }

        /// <summary>
        /// The post must return 400 bad request if the entity is invalid json.
        /// </summary>
        [TestMethod]
        public void PostMustReturn400BadRequestIfTheEntityIsInvalidJson()
        {
            PostMustReturn400BadRequestIfTheEntityIsInvalid(JsonContentType);
        }

        /// <summary>
        /// The post must return 400 bad request if the entity is invalid xml.
        /// </summary>
        [TestMethod]
        public void PostMustReturn400BadRequestIfTheEntityIsInvalidXml()
        {
            PostMustReturn400BadRequestIfTheEntityIsInvalid(XmlContentType);
        }

        /// <summary>
        /// The post must return 409 conflict if the entity conflicts with another entity json.
        /// </summary>
        [TestMethod]
        public void PostMustReturn409ConflictIfTheEntityConflictsWithAnotherEntityJson()
        {
            PostMustReturn409ConflictIfTheEntityConflictsWithAnotherEntity(JsonContentType);
        }

        /// <summary>
        /// The post must return 409 conflict if the entity conflicts with another entity xml.
        /// </summary>
        [TestMethod]
        public void PostMustReturn409ConflictIfTheEntityConflictsWithAnotherEntityXml()
        {
            PostMustReturn409ConflictIfTheEntityConflictsWithAnotherEntity(XmlContentType);
        }

        /// <summary>
        /// The put may add a new entity using the key provided in the uri json.
        /// </summary>
        [TestMethod]
        public void PutMayAddANewEntityUsingTheKeyProvidedInTheUriJson()
        {
            PutMayAddANewEntityUsingTheKeyProvidedInTheUri(JsonContentType);
        }

        /// <summary>
        /// The put may add a new entity using the key provided in the uri xml.
        /// </summary>
        [TestMethod]
        public void PutMayAddANewEntityUsingTheKeyProvidedInTheUriXml()
        {
            PutMayAddANewEntityUsingTheKeyProvidedInTheUri(XmlContentType);
        }

        /// <summary>
        /// The put must be idempotent add or update json.
        /// </summary>
        [TestMethod]
        public void PutMustBeIdempotentAddOrUpdateJson()
        {
            PutMustBeIdempotent(JsonContentType, AddOrUpdateServiceUri);
        }

        /// <summary>
        /// The put must be idempotent add or update xml.
        /// </summary>
        [TestMethod]
        public void PutMustBeIdempotentAddOrUpdateXml()
        {
            PutMustBeIdempotent(XmlContentType, AddOrUpdateServiceUri);
        }

        /// <summary>
        /// The put must be idempotent json.
        /// </summary>
        [TestMethod]
        public void PutMustBeIdempotentJson()
        {
            PutMustBeIdempotent(JsonContentType);
        }

        /// <summary>
        /// The put must be idempotent xml.
        /// </summary>
        [TestMethod]
        public void PutMustBeIdempotentXml()
        {
            PutMustBeIdempotent(XmlContentType);
        }

        /// <summary>
        /// The put must respect the precondition if match add or update json.
        /// </summary>
        [TestMethod]
        public void PutMustRespectThePreconditionIfMatchAddOrUpdateJson()
        {
            PutShouldRespectIfMatch(JsonContentType, AddOrUpdateServiceUri);
        }

        /// <summary>
        /// The put must respect the precondition if match add or update xml.
        /// </summary>
        [TestMethod]
        public void PutMustRespectThePreconditionIfMatchAddOrUpdateXml()
        {
            PutShouldRespectIfMatch(XmlContentType, AddOrUpdateServiceUri);
        }

        /// <summary>
        /// The put must respect the precondition if match json.
        /// </summary>
        [TestMethod]
        public void PutMustRespectThePreconditionIfMatchJson()
        {
            PutShouldRespectIfMatch(JsonContentType);
        }

        /// <summary>
        /// The put must respect the precondition if match xml.
        /// </summary>
        [TestMethod]
        public void PutMustRespectThePreconditionIfMatchXml()
        {
            PutShouldRespectIfMatch(XmlContentType);
        }

        /// <summary>
        /// The put must update the entity identified by the uri if it exists add or update json.
        /// </summary>
        [TestMethod]
        public void PutMustUpdateTheEntityIdentifiedByTheUriIfItExistsAddOrUpdateJson()
        {
            PutMustUpdateTheEntityIdentifiedByTheUriIfItExistsAddOrUpdate(JsonContentType);
        }

        /// <summary>
        /// The put must update the entity identified by the uri if it exists add or update xml.
        /// </summary>
        [TestMethod]
        public void PutMustUpdateTheEntityIdentifiedByTheUriIfItExistsAddOrUpdateXml()
        {
            PutMustUpdateTheEntityIdentifiedByTheUriIfItExistsAddOrUpdate(XmlContentType);
        }

        /// <summary>
        /// The put must update the entity identified by the uri if it exists json.
        /// </summary>
        [TestMethod]
        public void PutMustUpdateTheEntityIdentifiedByTheUriIfItExistsJson()
        {
            PutMustUpdateTheEntityIdentifiedByTheUriIfItExists(JsonContentType);
        }

        /// <summary>
        /// The put must update the entity identified by the uri if it exists xml.
        /// </summary>
        [TestMethod]
        public void PutMustUpdateTheEntityIdentifiedByTheUriIfItExistsXml()
        {
            PutMustUpdateTheEntityIdentifiedByTheUriIfItExists(XmlContentType);
        }

        /// <summary>
        /// The put with null resource will return bad request.
        /// </summary>
        [TestMethod]
        public void PutWithNullResourceWillReturnBadRequest()
        {
            // Arrange

            // Act
            var result = PutResource(1, null);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, result.Response.StatusCode);
        }

        /// <summary>
        /// The put with null resource will return bad request add or update.
        /// </summary>
        [TestMethod]
        public void PutWithNullResourceWillReturnBadRequestAddOrUpdate()
        {
            // Arrange

            // Act
            var result = PutResource(1, null, AddOrUpdateServiceUri);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, result.Response.StatusCode);
        }

        #endregion

        #region Methods

        /// <summary>
        /// The add if match header.
        /// </summary>
        /// <param name="deleteRequest">
        /// The delete request.
        /// </param>
        /// <param name="tag">
        /// The tag.
        /// </param>
        private static void AddIfMatchHeader(HttpRequestMessage deleteRequest, string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                deleteRequest.Headers.IfMatch.Add(new EntityTagHeaderValue((QuotedString)tag));
            }
        }

        /// <summary>
        /// The create http client.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <returns>
        /// The HttpClient
        /// </returns>
        private static HttpClient CreateHttpClient(string contentType = XmlContentType)
        {
            var client = new HttpClient();
            if (!string.IsNullOrEmpty(contentType))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            }

            return client;
        }

        /// <summary>
        /// The delete resource.
        /// </summary>
        /// <param name="resourceKey">
        /// The resource key.
        /// </param>
        /// <param name="tag">
        /// The tag.
        /// </param>
        /// <returns>
        /// the test result
        /// </returns>
        private static TestResult DeleteResource(int resourceKey, string tag = null)
        {
            using (var client = CreateHttpClient())
            {
                var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, GetResourceKeyUri(resourceKey));
                AddIfMatchHeader(deleteRequest, tag);

                var response = client.Send(deleteRequest);

                return new TestResult(response);
            }
        }

        /// <summary>
        /// The get must return a resource given a key if the resource with that key exists.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void GetMustReturnAResourceGivenAKeyIfTheResourceWithThatKeyExists(string contentType)
        {
            // Arrange
            const int ExpectedKey = 1;

            // Act
            var result = GetResource(ExpectedKey, contentType);
            result.Response.EnsureSuccessStatusCode();

            // Assert
            Assert.AreEqual(ExpectedKey, result.Sample.Key);
            Assert.AreEqual(contentType, result.Response.Content.Headers.ContentType.MediaType);
        }

        /// <summary>
        /// The get must return resources starting with the first one when skip is not defined.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void GetMustReturnResourcesStartingWithTheFirstOneWhenSkipIsNotDefined(string contentType)
        {
            // Arrange
            const int ExpectedKey = 1;
            const int Top = 3;

            var result = GetResourceSet(0, Top, contentType);

            // Assert
            Assert.AreEqual(contentType, result.Response.Content.Headers.ContentType.MediaType);
            Assert.AreEqual(ExpectedKey, result.ResourceSet[0].Key);
        }

        /// <summary>
        /// The get must return zero or more resources when take is not provided.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void GetMustReturnZeroOrMoreResourcesWhenTakeIsNotProvided(string contentType)
        {
            // Arrange
            const int Skip = 5;

            // Act
            var result = GetResourceSet(Skip, null, contentType);

            // Assert
            Assert.AreEqual(contentType, result.Response.Content.Headers.ContentType.MediaType);
            Assert.IsTrue(result.ResourceSet.Length > 0);
        }

        /// <summary>
        /// The get must return zero resources when skip is greater than the number of resources in the collection.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void GetMustReturnZeroResourcesWhenSkipIsGreaterThanTheNumberOfResourcesInTheCollection(
            string contentType)
        {
            // Arrange
            const int Skip = int.MaxValue;
            const int Top = 3;

            // Act
            var result = GetResourceSet(Skip, Top, contentType);

            // Assert
            Assert.AreEqual(contentType, result.Response.Content.Headers.ContentType.MediaType);
            Assert.AreEqual(0, result.ResourceSet.Length);
        }

        /// <summary>
        /// The get must skip skip resources in the collection and return up to take resources.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void GetMustSkipSkipResourcesInTheCollectionAndReturnUpToTakeResources(string contentType)
        {
            // Arrange
            const int Skip = 1;
            const int Top = 3;

            var result = GetResourceSet(Skip, Top, contentType);

            // Assert
            Assert.AreEqual(contentType, result.Response.Content.Headers.ContentType.MediaType);
            Assert.AreEqual(Top, result.ResourceSet.Length);
        }

        /// <summary>
        /// The get resource.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <returns>
        /// The result of the call
        /// </returns>
        private static TestResult GetResource(int key, string contentType = XmlContentType)
        {
            return GetResource(key, null, contentType);
        }

        /// <summary>
        /// The get resource.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="etag">
        /// The e tag.
        /// </param>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <returns>
        /// The result of the call
        /// </returns>
        private static TestResult GetResource(int key, EntityTagHeaderValue etag, string contentType = XmlContentType)
        {
            using (var client = CreateHttpClient(contentType))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, GetResourceKeyUri(key));
                if (etag != null)
                {
                    request.Headers.IfNoneMatch.Add(etag);
                }

                var response = client.Send(request);
                return new TestResult(response);
            }
        }

        /// <summary>
        /// Creates a URI for a resource
        /// </summary>
        /// <param name="expectedKey">
        /// The expected key.
        /// </param>
        /// <param name="baseUri">
        /// The base Uri.
        /// </param>
        /// <returns>
        /// The generated Uri.
        /// </returns>
        private static string GetResourceKeyUri(int expectedKey, string baseUri = null)
        {
            return string.Format("{0}/{1}", baseUri ?? ServiceUri, expectedKey);
        }

        /// <summary>
        /// Gets a resource set
        /// </summary>
        /// <param name="skip">
        /// The skip.
        /// </param>
        /// <param name="top">
        /// The top.
        /// </param>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <returns>
        /// The test result set
        /// </returns>
        private static TestResultSet GetResourceSet(int? skip, int? top, string contentType = XmlContentType)
        {
            using (var client = CreateHttpClient(contentType))
            {
                // Act
                var response = client.Get(GetResourceSetUri(skip, top));
                return new TestResultSet(response);
            }
        }

        /// <summary>
        /// Builds a Uri with appropriate Skip / Take parameters
        /// </summary>
        /// <param name="skip">
        /// Records to skip
        /// </param>
        /// <param name="top">
        /// Records to take
        /// </param>
        /// <returns>
        /// The get resource set uri.
        /// </returns>
        private static string GetResourceSetUri(int? skip, int? top)
        {
            var index = 0;
            var sb = new StringBuilder("{0}/");

            if (skip.HasValue || top.HasValue)
            {
                sb.Append("?");
            }

            if (skip.HasValue)
            {
                index++;
                sb.AppendFormat("$skip={{{0}}}", index);
            }

            if (index > 0)
            {
                sb.Append("&");
            }

            if (top.HasValue)
            {
                index++;
                sb.AppendFormat("$top={{{0}}}", index);
            }

            if (skip.HasValue && top.HasValue)
            {
                return string.Format(sb.ToString(), ServiceUri, skip, top);
            }

            return string.Format(sb.ToString(), ServiceUri, skip.HasValue ? skip : top);
        }

        /// <summary>
        /// The parse key from location.
        /// </summary>
        /// <param name="pathAndQuery">
        /// The path and query.
        /// </param>
        /// <returns>
        /// The key from the location header
        /// </returns>
        private static int ParseKeyFromLocation(string pathAndQuery)
        {
            var paths = pathAndQuery.Split('/');
            return int.Parse(paths.Last());
        }

        /// <summary>
        /// The post.
        /// </summary>
        /// <param name="sample">
        /// The sample.
        /// </param>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <returns>
        /// The TestResult
        /// </returns>
        private static TestResult Post(Sample sample, string contentType = XmlContentType)
        {
            using (var client = CreateHttpClient(contentType))
            {
                return
                    new TestResult(
                        client.Post(ServiceUri, new ObjectContent(typeof(Sample), sample,contentType)));
            }
        }

        /// <summary>
        /// The post must append a valid resource to the resource collection.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void PostMustAppendAValidResourceToTheResourceCollection(string contentType)
        {
            // Arrange
            const string ExpectedData = "Post Data";

            var expectedResource = new Sample { Data = ExpectedData };

            var result = Post(expectedResource, contentType);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, result.Response.StatusCode);

            // Check entity
            Assert.AreEqual(ExpectedData, result.Sample.Data);

            // Check headers
            Assert.IsNotNull(result.Response.Headers.ETag, "Null etag");
            Assert.IsNotNull(result.Response.Headers.Location, "Null location");

            // Check server generated key and location header
            Assert.AreEqual(
                result.Sample.Key, 
                ParseKeyFromLocation(result.Response.Headers.Location.PathAndQuery), 
                "Location header key should match entity key");
            Assert.IsTrue(result.Sample.Key > 5, "Server generated key should be > 5 on test data set");
        }

        /// <summary>
        /// The post must ignore writes to entity fields the server considers read only.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void PostMustIgnoreWritesToEntityFieldsTheServerConsidersReadOnly(string contentType)
        {
            // Arrange
            const string ExpectedData = "Post Data";
            const string NotExpectedData = "Updated read only data";
            //TODO: Figure out what to do with the readonly data
            //var expectedResource = new Sample { Data = ExpectedData, ReadOnlyData = NotExpectedData };
            var expectedResource = new Sample { Data = ExpectedData };
            // Act
            var result = Post(expectedResource, contentType);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, result.Response.StatusCode);
            Assert.AreNotEqual(NotExpectedData, result.Sample.ReadOnlyData);
        }

        /// <summary>
        /// The post must return 400 bad request if the entity is invalid.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void PostMustReturn400BadRequestIfTheEntityIsInvalid(string contentType)
        {
            // Arrange
            var expectedData = string.Empty;
            new Sample { Data = expectedData };

            var result = Post(new Sample { Data = expectedData }, contentType);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, result.Response.StatusCode);
        }

        /// <summary>
        /// The post must return 409 conflict if the entity conflicts with another entity.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void PostMustReturn409ConflictIfTheEntityConflictsWithAnotherEntity(string contentType)
        {
            // Arrange
            const string ExpectedData = "Post Data";
            var expectedResource = new Sample { Data = ExpectedData };

            // Act
            var result1 = Post(expectedResource, contentType);
            var result2 = Post(expectedResource, contentType);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, result1.Response.StatusCode);
            Assert.AreEqual(HttpStatusCode.Conflict, result2.Response.StatusCode);
        }

        /// <summary>
        /// The put may add a new entity using the key provided in the uri.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void PutMayAddANewEntityUsingTheKeyProvidedInTheUri(string contentType)
        {
            // Arrange
            const int ResourceKey = 333;
            var expectedData = "Sample" + ResourceKey;
            var putResource = new Sample { Data = expectedData };
            var expectedUri = new Uri(GetResourceKeyUri(ResourceKey, AddOrUpdateServiceUri));

            // Act
            var result = PutResource(ResourceKey, putResource, AddOrUpdateServiceUri, contentType);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, result.Response.StatusCode, "Resource should be added");
            Assert.AreEqual(expectedData, result.Sample.Data, "Added Resource was not returned correctly");
            Assert.AreEqual(
                expectedUri.PathAndQuery, 
                result.Response.Headers.Location.PathAndQuery, 
                "Location header was not set correctly");
            Assert.IsNotNull(result.Response.Headers.ETag, "Response should include etag");
        }

        /// <summary>
        /// The put must be idempotent.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <param name="baseUri">
        /// The base uri.
        /// </param>
        private static void PutMustBeIdempotent(string contentType, string baseUri = null)
        {
            // Arrange
            const int ResourceKey = 1;
            var getResult = GetResource(ResourceKey);
            getResult.Response.EnsureSuccessStatusCode();

            // Act
            var putResource = getResult.Sample;
            putResource.Data = "modified";

            // This will modify the etag
            var put1Result = PutResource(ResourceKey, putResource, baseUri, contentType);
            put1Result.Response.EnsureSuccessStatusCode();

            var putResource2 = put1Result.Sample;

            // Put the same resource again
            var put2Result = PutResource(ResourceKey, putResource2, baseUri, contentType);
            put2Result.Response.EnsureSuccessStatusCode();

            // Assert
            Assert.AreEqual(
                put1Result.Response.Headers.ETag.Tag, 
                put2Result.Response.Headers.ETag.Tag, 
                "ETags should not change when put the same resource twice");
        }

        /// <summary>
        /// The put must update resource if exists.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <param name="baseUri">
        /// The base uri.
        /// </param>
        private static void PutMustUpdateResourceIfExists(string contentType, string baseUri = null)
        {
            // Arrange
            const int ExpectedKey = 1;
            const string ExpectedData = "modified";
            var resultGet = GetResource(ExpectedKey);
            var putResource = resultGet.Sample;
            putResource.Data = "modified";

            // Act
            var resultPut = PutResource(ExpectedKey, putResource, baseUri, contentType);
            resultPut.Response.EnsureSuccessStatusCode();

            // Assert
            Assert.AreEqual(ExpectedData, resultPut.Sample.Data);
            Assert.AreNotEqual(
                resultGet.Response.Headers.ETag, resultPut.Response.Headers.ETag, "Entity tags should have changed");
            Assert.AreNotEqual(resultGet.Sample.Tag, resultPut.Sample.Tag, "Sample version should have changed");
        }

        /// <summary>
        /// The put must update the entity identified by the uri if it exists.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void PutMustUpdateTheEntityIdentifiedByTheUriIfItExists(string contentType)
        {
            PutMustUpdateResourceIfExists(contentType);
        }

        /// <summary>
        /// The put must update the entity identified by the uri if it exists add or update.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        private static void PutMustUpdateTheEntityIdentifiedByTheUriIfItExistsAddOrUpdate(string contentType)
        {
            PutMustUpdateResourceIfExists(contentType, AddOrUpdateServiceUri);
        }

        /// <summary>
        /// The put resource.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="putResource">
        /// The resource to put
        /// </param>
        /// <param name="baseUri">
        /// The base uri.
        /// </param>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <returns>
        /// The test result
        /// </returns>
        private static TestResult PutResource(
            int key, Sample putResource, string baseUri = null, string contentType = XmlContentType)
        {
            using (var client = CreateHttpClient(contentType))
            {
                var request = new HttpRequestMessage(HttpMethod.Put, GetResourceKeyUri(key, baseUri))
                    {
                       Content = new ObjectContent(typeof(Sample), putResource, contentType) 
                    };

                if (putResource != null && !string.IsNullOrWhiteSpace(putResource.Tag))
                {
                    request.Headers.IfMatch.Add(new EntityTagHeaderValue((QuotedString)putResource.Tag));
                }

                var response = client.Send(request);

                return new TestResult(response);
            }
        }

        /// <summary>
        /// The put should respect if match.
        /// </summary>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <param name="baseUri">
        /// The base uri.
        /// </param>
        private static void PutShouldRespectIfMatch(string contentType, string baseUri = null)
        {
            // Arrange
            const int ResourceKey = 1;

            var resultGet = GetResource(ResourceKey);

            var putResource = resultGet.Sample;
            putResource.Data = "modified";

            // Act
            // Update the resource - will modify the tag
            var resultPut1 = PutResource(ResourceKey, putResource, baseUri, contentType);
            resultPut1.Response.EnsureSuccessStatusCode();

            // Try to update it again - should fail precondition
            var resultPut2 = PutResource(ResourceKey, putResource, baseUri, contentType);

            // Assert
            Assert.AreEqual(HttpStatusCode.PreconditionFailed, resultPut2.Response.StatusCode);
        }

        #endregion

        /// <summary>
        /// The test result.
        /// </summary>
        private class TestResult
        {
            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="TestResult"/> class.
            /// </summary>
            /// <param name="response">
            /// The response.
            /// </param>
            public TestResult(HttpResponseMessage response)
            {
                this.Response = response;

                if (this.Response.IsSuccessStatusCode && this.Response.StatusCode != HttpStatusCode.NoContent)
                {
                    this.Sample = this.Response.Content.ReadAs<Sample>();
                }
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///   Gets Response.
            /// </summary>
            public HttpResponseMessage Response { get; private set; }

            /// <summary>
            ///   Gets Resource.
            /// </summary>
            public Sample Sample { get; private set; }

            #endregion
        }

        /// <summary>
        /// The test result set.
        /// </summary>
        private class TestResultSet
        {
            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="TestResultSet"/> class.
            /// </summary>
            /// <param name="response">
            /// The response.
            /// </param>
            internal TestResultSet(HttpResponseMessage response)
            {
                this.Response = response;

                if (this.Response.IsSuccessStatusCode && this.Response.StatusCode != HttpStatusCode.NoContent)
                {
                    this.ResourceSet = response.Content.ReadAs<Sample[]>();
                }
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///   Gets Response.
            /// </summary>
            public HttpResponseMessage Response { get; private set; }

            #endregion

            #region Properties

            /// <summary>
            ///   Gets ResourceSet.
            /// </summary>
            internal Sample[] ResourceSet { get; private set; }

            #endregion
        }
    }
}