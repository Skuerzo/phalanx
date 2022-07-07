using WarHub.ArmouryModel.Source;

namespace WarHub.ArmouryModel.Concrete;
internal class SelectionSymbol : RosterEntryBasedSymbol, ISelectionSymbol, INodeDeclaredSymbol<SelectionNode>
{
    public SelectionSymbol(
        ISymbol? containingSymbol,
        SelectionNode declaration,
        DiagnosticBag diagnostics)
        : base(containingSymbol, declaration, diagnostics)
    {
        Declaration = declaration;
        SourceEntryPath = new(this, declaration);
        Costs = declaration.Costs.Select(x => new CostSymbol(this, x, diagnostics)).ToImmutableArray();
        Resources = Costs.Cast<CostSymbol, ResourceEntryBaseSymbol>().AddRange(CreateRosterEntryResources(diagnostics));
        Categories = declaration.Categories.Select(x => new CategorySymbol(this, x, diagnostics)).ToImmutableArray();
        ChildSelections = declaration.Selections.Select(x => new SelectionSymbol(this, x, diagnostics)).ToImmutableArray();
        PrimaryCategory = Categories.FirstOrDefault(x => x.IsPrimaryCategory); // TODO diagnostic if count != 1 for root selection? (also what about NoCategory)
    }

    public override SelectionNode Declaration { get; }

    public override SymbolKind Kind => SymbolKind.Selection;

    public int Count => Declaration.Number;

    public SelectionReferencePathSymbol SourceEntryPath { get; }

    public override ISelectionEntrySymbol SourceEntry =>
        (ISelectionEntrySymbol)SourceEntryPath.SourceEntries.Last();

    public ImmutableArray<SelectionSymbol> ChildSelections { get; }

    public override ImmutableArray<ResourceEntryBaseSymbol> Resources { get; }

    public SelectionEntryKind EntryKind => Declaration.Type;

    public ICategorySymbol? PrimaryCategory { get; }

    public ImmutableArray<CategorySymbol> Categories { get; }

    public ImmutableArray<CostSymbol> Costs { get; }

    ISelectionReferencePathSymbol ISelectionSymbol.SourceEntryPath => SourceEntryPath;

    ImmutableArray<ICategorySymbol> ISelectionSymbol.Categories =>
        Categories.Cast<CategorySymbol, ICategorySymbol>();

    ImmutableArray<ICostSymbol> ISelectionSymbol.Costs =>
        Costs.Cast<CostSymbol, ICostSymbol>();

    ImmutableArray<ISelectionSymbol> IRosterSelectionTreeElementSymbol.ChildSelections =>
        ChildSelections.Cast<SelectionSymbol, ISelectionSymbol>();

    protected override ImmutableArray<Symbol> MakeAllMembers(BindingDiagnosticBag diagnostics) =>
        base.MakeAllMembers(diagnostics)
        .Add(SourceEntryPath)
        .AddRange(Categories)
        .AddRange(ChildSelections);
}
