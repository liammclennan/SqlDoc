Giv.n
=====

Giv.n is a BDD framework with all the extraneous parts removed. It supports the given, when, then style of scenario definition, including reuse and parameterization of steps ala FIT and fitness (but without the wiki).

Example
-------

(This example uses NUnit)

```c#
using Givn;

[Test]
public WhenIBakeACake() {
    Giv.n(IHaveFlour)
        .And(IHaveEggs);
    Wh.n(IBakeCake);
    Th.n(IHaveDeliciousSnack);
}

private void IHaveFlour() {
    _ingredients.Add(new Flour());
}

private void IHaveEggs() {
    _ingredients.Add(new Eggs());
}

private void IBakeCake() {
    _cake = _oven.Bake(_ingredients);
}

private void IHaveDeliciousSnack() {
    Assert.IsTrue(_cake.IsDelicious());
}
```