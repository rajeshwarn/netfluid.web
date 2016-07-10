# Netfluid.Web http://netfluid.org/
Easy embeddable application server to expose class an methods over the net

##Getting started
To expose a method all you need to do is to mark it with the Route attribute.
The route defines on wich URL the method will be invoked.

```
[Route("/my/rest")]
public static dynamic List()
{
    //When a method returns a class or an enumerable they are automatically JSON serialized
    return Database.Elements.All;
}
```

Distinguish HTTP methods:
```
[Route("/myclass")]
class MyClass
{
    [Route("/","POST")]public dynamic Create() { /*....*/ }
    [Route("/","GET")]public dynamic Read() { /*....*/ }
    [Route("/","POST")]public dynamic Update() { /*....*/ }
    [Route("/","GET")]public dynamic elete() { /*....*/ } 
}
```

Method parameters are automatically parsed from POST and GET arguments.
```
[Route("/myclass")]
class MyClass
{
    dynamic SignUp(string username, string password, EMailAddress address) { /*...*/ }
}
```

To enable the automatic parsing on self defined class, add the **public static T Parse(string)** method to the class
```
class User
{
    public string Id;
    public string Name;
    public string Password;
    
    public static User Parse(string id)
    {
        return Database.Where(x=>x.Id==id);
    }
}
```

To set in-URL parameters, mark them with **:**
```
[Route("/new/:date/:value")]
public static dynamic MyMthod(DateTime date, MyEnum value)
{
    /*....*/
}
```

