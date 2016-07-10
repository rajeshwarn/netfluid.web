# Netfluid.Web http://netfluid.org/
## Easy embeddable application server to expose class an methods over the net

###Getting started
To expose a method all you need to do is to mark it with the Route attribute

```
[Route("/my/rest")]
public static dynamic List()
{
    //When a method returns a class or an enumerable they are automatically JSON serialized
    return Database.Elements.All;
}
```

