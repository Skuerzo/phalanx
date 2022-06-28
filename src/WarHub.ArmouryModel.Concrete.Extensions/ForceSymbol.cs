using WarHub.ArmouryModel.Source;

namespace WarHub.ArmouryModel.Concrete;

internal class ForceSymbol : RosterEntryBasedSymbol, IForceSymbol, INodeDeclaredSymbol<ForceNode>
{
    private IForceEntrySymbol? lazyForceEntry;

    public ForceSymbol(
        ISymbol? containingSymbol,
        ForceNode declaration,
        DiagnosticBag diagnostics)
        : base(containingSymbol, declaration, diagnostics)
    {
        Declaration = declaration;
        CatalogueReference = new ForceCatalogueReferenceSymbol(this, declaration, diagnostics);
        Resources = CreateRosterEntryResources(diagnostics).ToImmutableArray();
        Categories = declaration.Categories.Select(x => new CategorySymbol(this, x, diagnostics)).ToImmutableArray();
        Publications = declaration.Publications.Select(x => new PublicationSymbol(this, x, diagnostics)).ToImmutableArray();
        ChildForces = declaration.Forces.Select(x => new ForceSymbol(this, x, diagnostics)).ToImmutableArray();
        ChildSelections = declaration.Selections.Select(x => new SelectionSymbol(this, x, diagnostics)).ToImmutableArray();
    }

    public override ForceNode Declaration { get; }

    public override SymbolKind Kind => SymbolKind.Force;

    public override IForceEntrySymbol SourceEntry => GetBoundField(ref lazyForceEntry);

    public ImmutableArray<ForceSymbol> ChildForces { get; }

    public ImmutableArray<SelectionSymbol> ChildSelections { get; }

    public override ImmutableArray<ResourceEntryBaseSymbol> Resources { get; }

    public ImmutableArray<CategorySymbol> Categories { get; }

    public ImmutableArray<PublicationSymbol> Publications { get; }

    public ForceCatalogueReferenceSymbol CatalogueReference { get; }

    ICatalogueReferenceSymbol IForceSymbol.CatalogueReference => CatalogueReference;

    ImmutableArray<IForceSymbol> IForceSymbol.ChildForces =>
        ChildForces.Cast<ForceSymbol, IForceSymbol>();

    ImmutableArray<ICategorySymbol> IForceSymbol.Categories =>
        Categories.Cast<CategorySymbol, ICategorySymbol>();

    ImmutableArray<IPublicationSymbol> IForceSymbol.Publications =>
        Publications.Cast<PublicationSymbol, IPublicationSymbol>();

    ImmutableArray<ISelectionSymbol> IRosterSelectionTreeElementSymbol.ChildSelections =>
        ChildSelections.Cast<SelectionSymbol, ISelectionSymbol>();

    protected override void BindReferencesCore(Binder binder, BindingDiagnosticBag diagnostics)
    {
        base.BindReferencesCore(binder, diagnostics);
        lazyForceEntry = binder.BindForceEntrySymbol(Declaration, diagnostics);
    }

    protected override ImmutableArray<Symbol> MakeAllMembers(BindingDiagnosticBag diagnostics) =>
        base.MakeAllMembers(diagnostics)
        .Add(CatalogueReference)
        .AddRange(Categories)
        .AddRange(Publications)
        .AddRange(ChildForces)
        .AddRange(ChildSelections);
}
