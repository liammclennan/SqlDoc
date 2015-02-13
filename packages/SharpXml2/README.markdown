
# SharpXml

*SharpXml* is an independent, dependency-free and fast .NET XML serialization
library. It is written in F# and is built on .NET 4.0.

[![build status](https://api.travis-ci.org/kongo2002/SharpXml.svg)][8]

The project is inspired by the great .NET JSON serializer
[ServiceStack.Text][1].


## Get it

You can get *SharpXml* either by installing via [nuget][5], by downloading the
precompiled binaries or by cloning the git repository from [github][7] and
compiling the library on your own.


### NuGet

SharpXml can be found and installed via [nuget][6]:

	PM> Install-Package SharpXml


### Download binaries

You can also download the latest precompiled binaries using the *downloads* page
on github:

- <https://github.com/kongo2002/SharpXml/downloads/>


### Source files

Alternatively you can clone the *git repository* and compile the project by
yourself:

	git clone git://github.com/kongo2002/SharpXml.git
	cd SharpXml\SharpXml
	msbuild


## Lean API

The API tries to appear small and descriptive at the same time:

```cs
// Serialization functions

string XmlSerializer.SerializeToString<T>(T element);
string XmlSerializer.SerializeToString(object element, Type targetType);

void XmlSerializer.SerializeToWriter<T>(TextWriter writer, T element);
void XmlSerializer.SerializeToWriter(TextWriter writer, object element, Type targetType);

// Deserialization functions

T XmlSerializer.DeserializeFromString<T>(string value);
object XmlSerializer.DeserializeFromString(string value, Type targetType);

T XmlSerializer.DeserializeFromReader<T>(TextReader reader);
object XmlSerializer.DeserializeFromReader(TextReader reader, Type targetType);

T XmlSerializer.DeserializeFromStream<T>(Stream stream);
object XmlSerializer.DeserializeFromStream(Stream stream, Type targetType);
```

### Supported types

*T* can be any .NET POCO type. Apart from others *SharpXml* supports all basic
collection types residing in `System.Collections`, `System.Collections.Generic`
and `System.Collections.Specialized`:

* List&lt;T&gt;
* Dictionary&lt;TKey, TValue&gt;
* ICollection&lt;T&gt;
* IEnumerable&lt;T&gt;
* IList
* HashSet&lt;T&gt;
* Nullable&lt;T&gt;
* ReadOnlyCollection&lt;T&gt;
* Queue&lt;T&gt;
* Stack&lt;T&gt;
* LinkedList&lt;T&gt;
* SortedSet&lt;T&gt;
* NameValueCollection
* HashTable
* ArrayList
* CLR Arrays

Moreover *SharpXml* supports serialization and deserialization of the basic
**F# types** being:

* F# records
* F# and CLR tuples
* Immutable F# lists
* Discriminated unions (since `v1.5.0.0`)


### Configuration

*SharpXml* intends to work in a convention based manner meaning that there
won't be too many configuration options to change its basic (de-)serialization
behavior. A few options to modify *SharpXml's* output exist anyways:

- `XmlConfig.IncludeNullValues`: Whether to include `null` values in the
  generated/serialized output (default: `false`)

- `XmlConfig.ExcludeTypeInfo`: Whether to include additional type information
  for dynamic or anonymous types (default: `false`)

- `XmlConfig.EmitCamelCaseNames`: Whether to convert property/type names into
  camel-case output, i.e. `MyClass -> "myClass"` (default: `false`)

- `XmlConfig.WriteXmlHeader`: Whether to include a XML header sequence (`<?xml
  ... ?>`) in the serialized output (default: `false`)

- `XmlConfig.ThrowOnError`: Whether to throw an exception on deserialization
  errors or silently ignore errors (default: `false`)

- `XmlConfig.UseAttributes`: Activates deserialization and serialization support
  for XML *attributes* (see below for more information) (default: `false`)


### Custom serialization

Although *SharpXml* comes with built-in support of all basic .NET types there
are two ways to modify its de-/serialization behavior. You can either add
custom serialization and/or deserialization logic by registering serialization
delegates for a specified type on the static `XmlConfig` class or you can modify
serialization of collections using the `XmlElementAttribute` in the
`SharpXml.Common` namespace.

Moreover the serialization and deserialization of struct types may be customized
by overriding the public `ToString()` method and/or providing a static
`ParseXml()` function.


#### Registering delegates

```cs
/// Register a serializer delegate for the specified type
void RegisterSerializer<T>(SerializerFunc func);

/// Register a deserializer delegate for the specified type
void RegisterDeserializer<T>(DeserializerFunc func);

/// Unregister the serializer delegate for the specified type
void UnregisterSerializer<T>();

/// Unregister the deserializer delegate for the specified type
void UnregisterDeserializer<T>();

/// Clear all registered custom serializer delegates
void ClearSerializers();

/// Clear all registered custom deserializer delegates
void ClearDeserializers();
```


#### XmlElementAttribute

The `XmlElementAttribute` in `SharpXml.Common` allows you to modify the default
serialization of .NET types using a few properties to choose from:

- `[XmlElement Name="..."]`: Override the default name of the property/class

- `[XmlElement ItemName="..."]`: Override the default name of collection's
  items (default: `"item"`)

- `[XmlElement KeyName="..."]`: Override the default name of keys in dictionary
  types (default: `"key"`)

- `[XmlElement ValueName="..."]`: Override the default name of values in
  dictionary types (default: `"value"`)

- `[XmlElement Namespace="..."]`: Defines a XML namespace attribute for the
  selected type or property (*Note:* this attribute is currently used for
  serialization of root types only)

  **Deprecated**: this property is *deprecated* as of version `1.4.0.0` of
  *SharpXml*. Please use the `XmlNamespaceAttribute` instead.


## XML format

In the following section I want to give a *short* description of the format
*SharpXml* generates and expects on deserialization.

The first thing to mention is that *public properties* are serialized and
deserialized only. Fields whether public or not are not serialized at the moment
and won't be in the future! Attributes placed inside the XML tags are not
supported either and are simply ignored. Apart from that serialization is pretty
straight-forward and your XML looks like you would probably expect it anyway
-- at least from my point of view :-)


### Basic serialization

```cs
public class MyClass
{
	public int Foo { get; set; }
	public string Bar { get; set; }
}

var test = new MyClass { Foo = 144, Bar = "I like SharpXml very much" };
```

An instance of the class above will be serialized like the following:

```xml
<MyClass>
	<Foo>144</Foo>
	<Bar>I like SharpXml very much</Bar>
</MyClass>
```

Using `XmlConfig.EmitCamelCaseNames = true;` the generated XML output would
look like this instead:

```xml
<myClass>
	<foo>144</foo>
	<bar>I like SharpXml very much</bar>
</myClass>
```


### Collections

```cs
public class ListClass
{
	public int Id { get; set; }
	public List<string> Items { get; set; }
}

var test = new ListClass
	{
		Id = 20,
		Items = new List<string> { "one", "two" }
	};
```

*SharpXml* will generate the following XML:

```xml
<ListClass>
	<Id>20</Id>
	<Items>
		<Item>one</Item>
		<Item>two</Item>
	</Items>
</ListClass>
```


### Key-value collections (dictionaries)

```cs
public class DictClass
{
	public int Id { get; set; }
	public Dictionary<string, int> Values { get; set; }
}

var test = new DictClass
	{
		Id = 753,
		Values = new Dictionary<string, int>
			{
				{ "ten", 10 },
				{ "eight", 8 }
			}
	};
```

The serialized output by *SharpXml* looks like the following:

```xml
<DictClass>
	<Id>753</Id>
	<Values>
		<Item>
			<Key>ten</Key>
			<Value>10</Value>
		</Item>
		<Item>
			<Key>eight</Key>
			<Value>8</Value>
		</Item>
	</Values>
</DictClass>
```


### Discriminated unions

Since SharpXml version `1.5.0.0` F\# discriminated unions are supported for
serialization and deserialization as well.

```fs
type UnionType =
	| First
	| Second of int
	| Third of string * int

let unions = [
	UnionType.First;
	UnionType.Second 20;
	UnionType.Third ("test", 30)
	]
```

The above list of F\# discriminated unions will be serialized like this:

```xml
<List>
	<UnionType>
		<First></First>
	</UnionType>
	<UnionType>
		<Second>20</Second>
	</UnionType>
	<UnionType>
		<Third>
			<Item1>test</Item1>
			<Item2>30</Item2>
		</Third>
	</UnionType>
</List>
```

**Note**: In all XML examples above indentation is added for convenience only.


### Using XmlElementAttribute

As mentioned before you can use the `XmlElementAttribute` to customize the
generated XML output which is especially useful for collection and dictionary
types.

```cs
[XmlElement("CustomClass")]
public class CustomDictClass
{
	public int Id { get; set; }

	[XmlElement(ItemName="Element", KeyName="String", ValueName="Int")]
	public Dictionary<string, int> Values { get; set; }
}

var test = new CustomDictClass
	{
		Id = 753,
		Values = new Dictionary<string, int>
			{
				{ "ten", 10 },
				{ "eight", 8 }
			}
	};
```

This example shows the effect of the four major options given by the
`XmlElementAttribute`: `Name`, `ItemName`, `KeyName` and `ValueName`.

```xml
<CustomClass>
	<Id>753</Id>
	<Values>
		<Element>
			<String>ten</String>
			<Int>10</Int>
		</Element>
		<Element>
			<String>eight</String>
			<Int>8</Int>
		</Element>
	</Values>
</CustomClass>
```


#### Root type namespaces

Using the property `Namespace` of the `XmlElementAttribute` you can set an
optional namespace string that will be used on serialization of the root element
of the resulting XML document:

**Deprecated**: this property is *deprecated* as of version `1.4.0.0` of
*SharpXml*. Please use the `XmlNamespaceAttribute` instead!

```cs
[XmlElement(Namespace = "Some.Namespace")]
public class NamespaceClass
{
	public int Id { get; set; }
	public string Name { get; set; }
}

var test = new NamespaceClass { Id = 201, Name = "foo" };
```

The class described above will be serialized like the following:

```xml
<NamespaceClass xmlns="Some.Namespace">
	<Id>201</Id>
	<Name>foo</Name>
</NamespaceClass>
```


#### Static attribute values

Like mentioned before instead of using the attribute `XmlElementAttribute` to
set a namespace at the root level you can use `XmlNamespaceAttribute` instead.
This attribute class is supported since *SharpXml 1.4.0.0*.

You can achieve the same result of above like this:

```cs
[XmlNamespace("xmlns=\"Some.Namespace\"")]
public class NamespaceClass
{
	public int Id { get; set; }
	public string Name { get; set; }
}
```

Like this you can set multiple static attributes as well:

```cs
[XmlNamespace("xmlns=\"Some.Namespace\"", "version=\"2.3.4.0\"")]
public class MultipleNamespaceClass
{
	public int Id { get; set; }
	public string Name { get; set; }
}
```

*Note*: These attribute values are static and are used for serialization only.
There is no actual matching or any validation logic against XML namespaces
during the deserialization process.


### Struct types

Non-reference types like struct may provide custom implementation of the methods
`ToString()` and/or `ParseXml()` in order to customize *SharpXml's*
serialization behavior.

A typical example might look like this:

```cs
public struct MyStruct
{
	public int X { get; set; }
	public int Y { get; set; }

	/// <summary>
	/// Custom ToString() implementation - will be used by SharpXml
	/// </summary>
	public override string ToString()
	{
		return X + "x" + Y;
	}

	/// <summary>
	/// Custom deserialization function used by SharpXml
	/// </summary>
	public static MyStruct ParseXml(string input)
	{
		var parts = input.Split('x');

		return new MyStruct
			{
				X = int.Parse(parts[0]),
				Y = int.Parse(parts[1])
			};
	}
}

var test = new MyStruct { X = 200, Y = 50 };
```

Using the struct type described above results in the following output:

```xml
<MyStruct>200x50</MyStruct>
```

Without the custom implementations the struct would be serialized like this:

```xml
<MyStruct>
	<X>200</X>
	<Y>50</Y>
</MyStruct>
```


### Custom serialization delegates

Moreover reference types can be customized by registering custom serialization
delegates to the static `XmlConfig` class using the aforementioned
`RegisterSerializer` and `RegisterDeserializer` functions.

```cs
public class SomeClass
{
	public double Width { get; set; }
	public double Height { get; set; }
}

// register custom serializer
XmlConfig.RegisterSerializer<SomeClass>(x => return x.Width + "x" + x.Height);

// register custom deserializer
XmlConfig.RegisterDeserializer<SomeClass>(v => {
		var parts = v.Split('x');
		return new SomeClass
			{
				Width = double.Parse(parts[0]),
				Height = double.Parse(parts[1])
			};
	});
```

The resulting XML will look pretty much the same as the struct example described
earlier but you can imagine the possibilities given by this approach.


### Deserialization

The deserialization logic of *SharpXml* can be described as very fault-tolerant
meaning that usually bad formatted or even invalid XML may be deserialized
without errors.

- Tag name matching is *case insensitive*

- Closing tags don't have to be the same as the opening tag. The nesting of tags
  is more important here.

- The order of the tags is irrelevant

- Tag attributes are ignored by default (since version `1.4.0.0.` XML attribute
  support can be enabled using `XmlConfig.UseAttributes`)

- XML namespaces are ignored as well

- XML parsing is not completely XML 1.0/1.1 compliant. I.e. `CDATA` sections are
  not supported yet while comment parsing is just rudimentary implemented.

In order to provide a better view on how fault-tolerant *SharpXml* works I will
give an example of a *very bad formatted* XML input that will be deserialized
without any errors:

```xml
<myclass>
	< foo >20</fo>
	<BAR attr="ignored anyway">ham eggs< /bar>
</MyClass>
```

This XML above will be successfully deserialized into an instance of `MyClass`.


### Type resolving

In case you do not know at compile time what type you have to deserialize the
XML data into you can use the specific overload of the `DeserializeFromString`,
`DeserializeFromReader` or `DeserializeFromStream` method that takes a
`TypeResolver` parameter. This way you can determine the type based on the tag
information of the XML root node.

This method may be especially useful in a web service scenario where you can
deserialize the incoming XML data into the appropriate type and route the DTO
into the specific handler routine. You could use this method like that:

```cs
using System;
using System.Reflection;

using SharpXml;

public interface IHandlerProvider
{
	Type DetermineType(XmlInfo info);
}

public class XmlHandlerProvider : IHandlerProvider
{
	public Type DetermineType(XmlInfo info)
	{
		// this would be some custom built logic to determine
		// the specific data type based on the given XML root node
		// information

		// this is just some dummy logic!

		var assembly = Assembly.GetExecutingAssembly();
		var type = assembly.GetType(info.Name);

		if (type == null)
		{
			var namespace = info.HasAttributes
				? info.Attributes.Find(a => a.Key == "xmlns")
				: null;

			if (namespace != null)
				type = assembly.GetType(namespace.Value);
		}

		if (type == null)
		{
			throw new NotSupportedException(
				string.Format("There is no handler for type '{0}'", info.Name));
		}

		return type;
	}
}

public void Process(string xmlString)
{
	IHandlerProvider provider = new XmlHandlerProvider();

	var data = XmlSerializer.DeserializeFromString(xmlString, provider.DetermineType);
}
```

The example above is just a dummy to give you an idea how this functionality
could be integrated. The `XmlInfo` class contains the root name and a list with
all of its attribute values (if `UseAttributes` is enabled).


### XML attributes

Since SharpXml version `1.4.0.0` XML attributes are supported as well although
not enabled by default. In order to use attributes in serialization and
deserialization you have to enable the setting `XmlConfig.UseAttributes`:

```cs
// activate XML attribute support
XmlConfig.Instance.UseAttributes = true;
```

It is important to initially set this property before you actually use the
`XmlSerializer`. Since all deserialization and serialization functions and type
information are cached at the first time for any type you have to make sure the
setting is set early. In case you have to reset the serializer cache you can
trigger a refresh manually:

```cs
// clear both serializers and deserializers
XmlSerializer.ClearCache();

// basically the same like this:
//XmlSerializer.ClearSerializers();
//XmlSerializer.ClearDeserializers();

XmlConfig.Instance.UseAttributes = true;
```

Moreover attributes have to be marked with the `XmlAttribute` attribute in the
`SharpXml.Common` namespace:


#### XmlAttribute

Use the `XmlAttribute` attribute to mark a specific property to be
serialized/deserialized as an attribute:

```cs
public class AttributeClass
{
	public string Value { get; set; }

	[XmlAttribute]
	public int Version { get; set; }

	[XmlAttribute("Key")]
	public string Name { get; set; }
}

var test = new AttributeClass
{
	Value = "Test",
	Version = 2,
	Name = "Attribute Test"
};
```

The above example would result in the following XML:

```xml
<AttributeClass Version="2" Key="Attribute Test">
	<Value>Test</Value>
</AttributeClass>

```

The ordering of the XML attributes on serialization can not be manipulated or
defined by the user.


#### XmlAttribute on List&lt;T&gt; types

Additionally it is supported to override the `List<T>` class in order to specify
attributes on a collection base. This may look like the following:

```cs
public class AttributeList<T> : List<T>
{
	[XmlAttribute]
	public int Version { get; set; }
}

public class AttributeTest
{
	[XmlAttribute("Attr")]
	public string Attribute { get; set; }

	public AttributeList<string> Values { get; set; }
}

var values = new AttributeList<string>
{
	"one",
	"two"
};

values.Version = 5;

var test = new AttributeTest
{
	Attribute = "value",
	Values = values
};
```

Here is the resulting XML output:

```xml
<AttributeTest Attr="value">
	<Values Version="5">
		<Item>one</Item>
		<Item>two</Item>
	</Values>
</AttributeTest>
```


## Todo

Some random things I am planning to work on in the future:

- Extend documentation/README
- Make `SharpXml.Common` an optional dependency
- Investigate into additional performance tweaks
- Additional unit tests
- Improve error messages/handling


## Maintainer

*SharpXml* is written by Gregor Uhlenheuer. You can reach me at
[kongo2002@gmail.com][3]


## License

*SharpXml* is licensed under the [Apache license][2], Version 2.0

> Unless required by applicable law or agreed to in writing, software
> distributed under the License is distributed on an "AS IS" BASIS,
> WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
> See the License for the specific language governing permissions and
> limitations under the License.

[1]: http://github.com/ServiceStack/ServiceStack.Text
[2]: http://www.apache.org/licenses/LICENSE-2.0
[3]: mailto:kongo2002@gmail.com
[4]: http://www.mono-project.com/
[5]: http://nuget.org/
[6]: http://nuget.org/packages/SharpXml/
[7]: https://github.com/kongo2002/SharpXml/
[8]: https://travis-ci.org/kongo2002/SharpXml/

<!-- vim: set noet ts=4 sw=4 sts=4 tw=80: -->
