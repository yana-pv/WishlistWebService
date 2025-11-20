using System.Net;

namespace WishLister.Middleware;

public delegate Task RequestDelegate(HttpListenerContext context);