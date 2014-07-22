using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace NetFluid.Site.OpenDb
{
    [VirtualHost("opendb.netfluid.org")]
    public class OpenDatabase:FluidPage
    {
        static readonly MongoDatabase database;

        static OpenDatabase()
        {
            var client = new MongoClient("mongodb://localhost");
            database = client.GetServer().GetDatabase("opendb");
        }

        BsonDocument GetDocument()
        {
            var document = new BsonDocument();

            if (Request.Headers["content-type"].Contains("json"))
            {
                return BsonDocument.Parse(Context.Reader.ReadToEnd());
            }

            foreach (var prop in Request.Values)
            {
                if (prop.IsMultiple)
                {
                    document.Add(prop.Name, new BsonArray(prop.ToArray()));
                }
                else
                {
                    document.Add(prop.Name, prop.Value);
                }
            }

            return document;
        }

        [Route("/")]
        public IResponse Index()
        {
            return new FluidTemplate("./OpenDb/index.html");
        }

        [Route("/loadUrl")]
        public IResponse LoadUrl(string url,string collection, string id,string token)
        {
            if (collection == null)
                return new JSONResponse(database.GetCollectionNames());

            if (id == null)
                return new JSONResponse(database.GetCollection<Slot>(collection).FindAll().SetFields("OpenId").Select(x => x.OpenId));

            var r = database.GetCollection<Slot>(collection).FindOne(Query.EQ("OpenId", id));
            var wc = new System.Net.WebClient();
            var document = BsonDocument.Parse(wc.DownloadString(url));

            if (r == null)
            {
                switch (Request.HttpMethod)
                {
                    case "PUT":
                    case "POST":
                        var doc = document;
                        database.GetCollection<Slot>(collection).Save(new Slot
                        {
                            Document = doc,
                            OpenId = id,
                            Timestamp = DateTime.Now,
                            Token = token
                        });
                        return new JSONResponse(doc.ToJson());
                    case "GET":
                    case "DELETE":
                        return new JSONResponse("{}");
                }
                Response.StatusCode = StatusCode.MethodNotAllowed;
                return new JSONResponse(new Error { Code = 101, Message = "Invalid method " + Request.HttpMethod });
            }

            switch (Request.HttpMethod)
            {
                case "PUT":
                case "POST":
                    if (token == null || token != r.Token)
                    {
                        Response.StatusCode = StatusCode.Forbidden;
                        return new JSONResponse(new Error { Code = 102, Message = "Invalid or missing access token" });
                    }
                    r.Document = document;
                    r.Timestamp = DateTime.Now;
                    return new JSONResponse(r.Document.ToJson());
                case "DELETE":
                    if (token == null || token != r.Token)
                    {
                        Response.StatusCode = StatusCode.Forbidden;
                        return new JSONResponse(new Error { Code = 102, Message = "Invalid or missing access token" });
                    }
                    database.GetCollection(collection).Remove(Query.EQ("OpenId", id));
                    return new JSONResponse(r.Document == null ? "{}" : r.Document.ToJson());
                case "GET":
                    return new JSONResponse(r.Document == null ? "{}" : r.Document.ToJson());
                default:
                    return new JSONResponse(new Error { Code = 101, Message = "Invalid method " + Request.HttpMethod });
            }
        }

        [ParametrizedRoute("db")]
        public JSONResponse Database(string collection,string id,string password)
        {
            if(collection==null)
                return new JSONResponse(database.GetCollectionNames());

            if (id == null)
                return new JSONResponse(database.GetCollection<Slot>(collection).FindAll().SetFields("OpenId").Select(x=>x.OpenId));

            var r = database.GetCollection<Slot>(collection).FindOne(Query.EQ("OpenId", id));

            if (r == null)
            {
                switch (Request.HttpMethod)
                {
                    case "PUT":
                    case "POST":
                        var doc = GetDocument();
                        database.GetCollection<Slot>(collection).Save(new Slot
                        {
                            Document = doc,
                            OpenId = id,
                            Timestamp = DateTime.Now,
                            Token = password
                        });
                        return new JSONResponse(doc.ToJson());
                    case "GET":
                    case "DELETE":
                        return new JSONResponse("{}");
                }
                Response.StatusCode = StatusCode.MethodNotAllowed;
                return new JSONResponse(new Error{Code = 101, Message = "Invalid method "+Request.HttpMethod});
            }

            switch (Request.HttpMethod)
            {
                case "PUT":
                case "POST":
                    if (password == null || password != r.Token)
                    {
                        Response.StatusCode = StatusCode.Forbidden;
                        return new JSONResponse(new Error { Code = 102, Message = "Invalid or missing access token" });
                    }
                    r.Document = GetDocument();
                    r.Timestamp = DateTime.Now;
                    return new JSONResponse(r.Document.ToJson());
                case "DELETE":
                    if (password == null || password != r.Token)
                    {
                        Response.StatusCode = StatusCode.Forbidden;
                        return new JSONResponse(new Error { Code = 102, Message = "Invalid or missing access token" });
                    }
                    database.GetCollection(collection).Remove(Query.EQ("OpenId", id));
                    return new JSONResponse(r.Document == null ? "{}" : r.Document.ToJson());
                case "GET":
                   return new JSONResponse(r.Document == null ? "{}" : r.Document.ToJson());     
                default:
                    return new JSONResponse(new Error { Code = 101, Message = "Invalid method " + Request.HttpMethod });
            }
        }
    }
}
