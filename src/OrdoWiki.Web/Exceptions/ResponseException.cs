namespace OrdoWiki.Web.Exceptions;

public class ResponseException(string message) : InvalidOperationException(message);