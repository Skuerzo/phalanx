using WarHub.ArmouryModel.Source;

namespace WarHub.ArmouryModel.Concrete;

internal class CostTypeSymbol : SourceDeclaredSymbol, ICostTypeSymbol, INodeDeclaredSymbol<CostTypeNode>
{
    public CostTypeSymbol(
        ICatalogueSymbol containingSymbol,
        CostTypeNode declaration,
        DiagnosticBag diagnostics)
        : base(containingSymbol, declaration)
    {
        Declaration = declaration;
    }

    public override CostTypeNode Declaration { get; }

    public override SymbolKind Kind => SymbolKind.ResourceDefinition;

    public ResourceKind ResourceKind => ResourceKind.Cost;
}