﻿using EPiServer.Core;
using EPiServer.Reference.Commerce.Shared.Models;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Specialized;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EPiServer.Reference.Commerce.Shared.Services
{
    [ServiceConfiguration(typeof(IMailService), Lifecycle = ServiceInstanceScope.Transient)]
    public class MailService : IMailService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUrlResolver _urlResolver;
        private readonly IContentLoader _contentLoader;
        private readonly IHtmlDownloader _htmlDownloader;

        public MailService(IHttpContextAccessor httpContextBase, 
            IUrlResolver urlResolver, 
            IContentLoader contentLoader,
            IHtmlDownloader htmlDownloader)
        {
            _httpContextAccessor = httpContextBase;
            _urlResolver = urlResolver;
            _contentLoader = contentLoader;
            _htmlDownloader = htmlDownloader;
        }

        public void Send(ContentReference mailReference, NameValueCollection nameValueCollection, string toEmail, string language)
        {
            var body = GetHtmlBodyForMail(mailReference, nameValueCollection, language);
            var mailPage = _contentLoader.Get<MailBasePage>(mailReference);

            Send(mailPage.MailTitle, body, toEmail);
        }

        public string GetHtmlBodyForMail(ContentReference mailReference, NameValueCollection nameValueCollection, string language)
        {
            var urlBuilder = new UrlBuilder(_urlResolver.GetUrl(mailReference, language))
            {
                QueryCollection = nameValueCollection
            };

            var basePath = new Uri(_httpContextAccessor.HttpContext.Request.GetEncodedUrl()).GetLeftPart(UriPartial.Authority);
            var relativePath = urlBuilder.ToString();
            
            if (relativePath.StartsWith(basePath))
            {
                relativePath = relativePath.Substring(basePath.Length);
            }

            return _htmlDownloader.Download(basePath, relativePath);
        }

        public void Send(string subject, string body, string recipientMailAddress)
        {
            var message = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(recipientMailAddress);

            Send(message);
        }

        public void Send(MailMessage message)
        {
            using (var client = new SmtpClient())
            {
                // The SMTP host, port and sender e-mail address are configured
                // in the system.net section in web.config.
                client.Send(message);
            }
        }
    }
}