//-----------------------------------------------------------------------
// <copyright file="Docs.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using Raven.Abstractions.Extensions;
using Raven.Database.Extensions;
using Raven.Database.Server.Abstractions;
using Raven.Json.Linq;

namespace Raven.Database.Server.Responders
{
	public class Docs : AbstractRequestResponder
	{
		public override string UrlPattern
		{
			get { return "^/docs/?$"; }
		}

		public override string[] SupportedVerbs
		{
			get { return new[] {"GET", "POST"}; }
		}

		public override void Respond(IHttpContext context)
		{
			switch (context.Request.HttpMethod)
			{
				case "GET":
					Guid lastDocEtag = Guid.Empty;
					Database.TransactionalStorage.Batch(accessor =>
					{
						lastDocEtag = accessor.Staleness.GetMostRecentDocumentEtag();
					});

					if (context.MatchEtag(lastDocEtag))
					{
						context.SetStatusToNotModified();
					}
					else
					{
						context.WriteHeaders(new RavenJObject(), lastDocEtag);

						var startsWith = context.Request.QueryString["startsWith"];
						if (string.IsNullOrEmpty(startsWith))
							context.WriteJson(Database.GetDocuments(context.GetStart(), context.GetPageSize(Database.Configuration.MaxPageSize), context.GetEtagFromQueryString()));
						else
							context.WriteJson(Database.GetDocumentsWithIdStartingWith(
								startsWith,
								context.Request.QueryString["matches"],
								context.GetStart(),
								context.GetPageSize(Database.Configuration.MaxPageSize)));
					}
					break;
				case "POST":
					var json = context.ReadJson();
					var id = Database.Put(null, Guid.Empty, json,
					                      context.Request.Headers.FilterHeaders(),
					                      GetRequestTransaction(context));
					context.SetStatusToCreated("/docs/" + Uri.EscapeUriString(id.Key));
					context.WriteJson(id);
					break;
			}
		}
	}
}