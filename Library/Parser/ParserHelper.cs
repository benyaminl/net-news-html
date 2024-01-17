using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace net_news_html.Library.Parser;

public class ParserHelper
{
    public static IDocument ParseWithProxy(IDocument document)
    {
        var imgs = document.QuerySelectorAll<IHtmlImageElement>("img");
        var urls = document.QuerySelectorAll<IHtmlAnchorElement>("a");
        
        foreach (var i in imgs)
        {
            i.Source = "/Proxy/" + HttpUtility.UrlEncode(i.Source);
        }
        
        foreach (var u in urls)
        {
            u.Href = "/Proxy/" + HttpUtility.UrlEncode(u.Href);
        }
        
        return document;
    }

    public static IDocument RemoveAllComment(IDocument document)
    {
        foreach (var el in document.QuerySelectorAll("*"))
        {
            for (int i = 0; i < el.ChildNodes.Length; i++)
            {
                var node = el.ChildNodes[i];
                if (node.NodeType == NodeType.Comment)
                {
                    node.RemoveFromParent();
                }
            }
        }

        return document;
    }
}