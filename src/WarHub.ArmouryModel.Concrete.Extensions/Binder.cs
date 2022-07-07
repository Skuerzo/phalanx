using System.Diagnostics;
using WarHub.ArmouryModel.Source;

namespace WarHub.ArmouryModel.Concrete;

internal class Binder
{
    protected Binder(Compilation compilation)
    {
        Compilation = compilation;
        if (this is not BuckStopsHereBinder)
            throw new InvalidOperationException();
    }

    protected Binder(Binder next)
    {
        Next = next ?? throw new ArgumentNullException(nameof(next));
        Compilation = next.Compilation;
    }

    internal Compilation Compilation { get; }

    protected Binder? Next { get; }

    protected Binder NextRequired => Next ?? throw new InvalidOperationException("Must have Next!");

    internal virtual Symbol? ContainingSymbol => NextRequired.ContainingSymbol;

    internal virtual ContainerEntryBaseSymbol? ContainingContainerSymbol =>
        ContainingSymbol as ContainerEntryBaseSymbol ?? NextRequired.ContainingContainerSymbol;

    internal virtual EntrySymbol? ContainingEntrySymbol =>
        ContainingSymbol as EntrySymbol ?? NextRequired.ContainingEntrySymbol;

    internal virtual ForceSymbol? ContainingForceSymbol =>
        ContainingSymbol as ForceSymbol ?? NextRequired.ContainingForceSymbol;

    internal virtual SelectionSymbol? ContainingSelectionSymbol =>
        ContainingSymbol as SelectionSymbol ?? NextRequired.ContainingSelectionSymbol;

    internal IProfileTypeSymbol BindProfileTypeSymbol(ProfileNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<IProfileTypeSymbol, ErrorSymbols.ErrorProfileTypeSymbol>(
            node, diagnostics, node.TypeId, LookupOptions.ProfileTypeOnly);

    internal IPublicationSymbol BindPublicationSymbol(IPublicationReferencingNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<IPublicationSymbol, ErrorSymbols.ErrorPublicationSymbol>(
            (SourceNode)node, diagnostics, node.PublicationId, LookupOptions.PublicationOnly);

    internal ICharacteristicTypeSymbol BindCharacteristicTypeSymbol(CharacteristicNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ICharacteristicTypeSymbol, ErrorSymbols.ErrorCharacteristicTypeSymbol>(
            node, diagnostics, node.TypeId, LookupOptions.CharacteristicTypeOnly);

    internal IForceEntrySymbol BindForceEntrySymbol(ForceNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<IForceEntrySymbol, ErrorSymbols.ErrorForceEntrySymbol>(
            node, diagnostics, node.EntryId, LookupOptions.ForceEntryOnly);

    internal ICatalogueSymbol BindCatalogueSymbol(CatalogueLinkNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ICatalogueSymbol, ErrorSymbols.ErrorCatalogueSymbol>(
            node, diagnostics, node.TargetId, LookupOptions.CatalogueOnly);

    internal ICatalogueSymbol BindCatalogueSymbol(ForceNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ICatalogueSymbol, ErrorSymbols.ErrorCatalogueSymbol>(
            node, diagnostics, node.CatalogueId, LookupOptions.CatalogueOnly);

    internal ICatalogueSymbol BindGamesystemSymbol(CatalogueNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ICatalogueSymbol, ErrorSymbols.ErrorGamesystemSymbol>(
            node, diagnostics, node.GamesystemId, LookupOptions.CatalogueOnly);

    internal ICatalogueSymbol BindGamesystemSymbol(RosterNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ICatalogueSymbol, ErrorSymbols.ErrorGamesystemSymbol>(
            node, diagnostics, node.GameSystemId, LookupOptions.CatalogueOnly);

    internal ICostTypeSymbol BindCostTypeSymbol(CostNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ICostTypeSymbol, ErrorSymbols.ErrorCostTypeSymbol>(
            node, diagnostics, node.TypeId, LookupOptions.CostTypeOnly);

    internal IResourceEntrySymbol BindSharedResourceEntrySymbol(InfoLinkNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<IResourceEntrySymbol, ErrorSymbols.ErrorResourceEntrySymbol>(
            node,
            diagnostics, node.TargetId,
            LookupOptions.ResourceEntryOnly | LookupOptions.SharedOnly | node.Type switch
            {
                InfoLinkKind.InfoGroup => LookupOptions.ResourceGroupEntryOnly,
                InfoLinkKind.Profile => LookupOptions.ProfileEntryOnly,
                InfoLinkKind.Rule => LookupOptions.RuleEntryOnly,
                _ => LookupOptions.Default,
            });

    internal ISelectionEntryContainerSymbol BindSharedSelectionEntrySymbol(EntryLinkNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ISelectionEntryContainerSymbol, ErrorSymbols.ErrorSelectionEntryContainerSymbol>(
            node,
            diagnostics, node.TargetId,
            LookupOptions.ContainerEntryOnly | LookupOptions.SharedOnly | node.Type switch
            {
                EntryLinkKind.SelectionEntry => LookupOptions.SelectionEntryOnly,
                EntryLinkKind.SelectionEntryGroup => LookupOptions.SelectionGroupEntryOnly,
                _ => LookupOptions.Default,
            });

    internal ICategoryEntrySymbol BindCategoryEntrySymbol(CategoryLinkNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ICategoryEntrySymbol, ErrorSymbols.ErrorCategoryEntrySymbol>(
            node, diagnostics, node.TargetId, LookupOptions.CategoryEntryOnly);

    internal ICategoryEntrySymbol BindCategoryEntrySymbol(CategoryNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ICategoryEntrySymbol, ErrorSymbols.ErrorCategoryEntrySymbol>(
            node, diagnostics, node.EntryId, LookupOptions.CategoryEntryOnly);

    // TODO this definitely needs work, as it looks downward, into children
    internal ISelectionEntrySymbol BindSelectionEntryGroupDefaultEntrySymbol(SelectionEntryGroupNode node, BindingDiagnosticBag diagnostics) =>
        BindSimple<ISelectionEntrySymbol, ErrorSymbols.ErrorSelectionEntrySymbol>(
            node, diagnostics, node.DefaultSelectionEntryId, LookupOptions.SelectionEntryOnly);

    internal ImmutableArray<ISelectionEntryContainerSymbol> BindSelectionSourcePathSymbol(SelectionNode node, BindingDiagnosticBag diagnostics)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(node.EntryId));
        var parentSelection = ContainingSelectionSymbol?.Declaration == node
            ? ContainingSelectionSymbol.ContainingSymbol as ISelectionSymbol
            : ContainingSelectionSymbol;
        Debug.Assert((parentSelection as SourceDeclaredSymbol)?.Declaration != node); // we're not causing infinite recursion
        var ids = node.EntryId.Split("::").ToImmutableArray();
        if (ids.Length == 0)
        {
            return ImmutableArray.Create<ISelectionEntryContainerSymbol>(new ErrorSymbols.ErrorSelectionEntrySymbol()
            {
                ErrorInfo = diagnostics.Add(ErrorCode.ERR_NoBindingCandidates, node.GetLocation(), "Selection's entry ID was empty."),
            });
        }
        if (ids.Any(string.IsNullOrWhiteSpace))
        {
            diagnostics.Add(ErrorCode.ERR_GenericError, node.GetLocation(), "Empty element(s) in split EntryId.");
        }
        ISelectionEntryContainerSymbol? qualifier = null;
        var resultBuilder = ImmutableArray.CreateBuilder<ISelectionEntryContainerSymbol>();
        // copy over already resolved links from containing selection
        if (parentSelection is { SourceEntryPath: { SourceEntries.Length: > 1 } path })
        {
            resultBuilder.AddRange(path.SourceEntries.SkipLast(1));
            qualifier = path.SourceEntries[^2].ReferencedEntry;
        }
        var idsLeft = ids.AsSpan()[resultBuilder.Count..];
        for (var i = 0; i < idsLeft.Length; i++)
        {
            var idToBind = idsLeft[i];
            var opts = LookupOptions.SelectionEntryOnly | (qualifier is null ? LookupOptions.RootOnly : LookupOptions.Default);
            var entrySymbol = BindSimple<ISelectionEntryContainerSymbol, ErrorSymbols.ErrorSelectionEntrySymbol>(
                node, diagnostics, idToBind, opts, qualifier);
            resultBuilder.Add(entrySymbol);
            qualifier = entrySymbol.ReferencedEntry;
        }
        return resultBuilder.ToImmutable();
    }

    private TSymbol BindSimple<TSymbol, TErrorSymbol>(
        SourceNode node,
        BindingDiagnosticBag diagnostics,
        string? symbolId,
        LookupOptions options,
        ISymbol? qualifier = null)
        where TSymbol : ISymbol
        where TErrorSymbol : ErrorSymbols.ErrorSymbolBase, TSymbol, new()
    {
        if (symbolId is null)
        {
            return new TErrorSymbol()
            {
                ErrorInfo = diagnostics.Add(ErrorCode.ERR_NoBindingCandidates, node.GetLocation(), "Symbol ID was null."),
            };
        }
        var result = LookupResult.GetInstance();
        LookupSymbolsWithDelayedDiagnosing(result, symbolId, options, qualifier);
        var bindingResult = ResultSymbol(result, symbolId, node, diagnostics, out _, options);
        result.Free();
        if (bindingResult is ErrorSymbols.ErrorSymbolBase error and not TSymbol)
        {
            return new TErrorSymbol().WithErrorDetailsFrom(error);
        }
        return (TSymbol)bindingResult;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method is WIP")]
    internal SingleLookupResult CheckViability(
        ISymbol symbol,
        string symbolId,
        LookupOptions options,
        bool diagnose)
    {
        if (symbol.Id != symbolId)
        {
            return LookupResult.Empty();
        }
        if (options.HasFlag(LookupOptions.RootOnly) && !IsRootEntry(symbol))
        {
            return LookupResult.Empty();
        }
        if (options.HasFlag(LookupOptions.SharedOnly) && !IsSharedEntry(symbol))
        {
            return LookupResult.Empty();
        }
        if (options.HasFlag(LookupOptions.CatalogueOnly) && symbol.Kind != SymbolKind.Catalogue)
        {
            return LookupResult.Empty();
        }
        if (options.HasFlag(LookupOptions.ResoureDefinitionOnly))
        {
            if (symbol.Kind != SymbolKind.ResourceDefinition || symbol is not IResourceDefinitionSymbol rdSymbol)
            {
                return LookupResult.Empty();
            }
            var resourceKind = rdSymbol.ResourceKind;
            if (options.HasFlag(LookupOptions.PublicationOnly) && resourceKind != ResourceKind.Publication)
            {
                return LookupResult.Empty();
            }
            if (options.HasFlag(LookupOptions.CostTypeOnly) && resourceKind != ResourceKind.Cost)
            {
                return LookupResult.Empty();
            }
            if (options.HasFlag(LookupOptions.ProfileTypeOnly) && resourceKind != ResourceKind.Profile)
            {
                return LookupResult.Empty();
            }
            if (options.HasFlag(LookupOptions.CharacteristicTypeOnly) && resourceKind != ResourceKind.Characteristic)
            {
                return LookupResult.Empty();
            }
        }
        if (options.HasFlag(LookupOptions.ResourceEntryOnly) && symbol.Kind != SymbolKind.Resource)
        {
            return LookupResult.Empty();
        }
        if (options.HasFlag(LookupOptions.ContainerEntryOnly) && symbol.Kind != SymbolKind.ContainerEntry)
        {
            return LookupResult.Empty();
        }
        return LookupResult.Good(symbol);
    }

    internal void CheckViability<TSymbol>(
        LookupResult result,
        ImmutableArray<TSymbol> symbols,
        string symbolId,
        LookupOptions options,
        bool diagnose)
        where TSymbol : ISymbol
    {
        foreach (var symbol in symbols)
        {
            result.MergeEqual(CheckViability(symbol, symbolId, options, diagnose));
        }
    }

    internal static ImmutableArray<ICatalogueSymbol> CalculateRootClosure(ICatalogueSymbol catalogue)
    {
        var closureItems = new List<ICatalogueSymbol>();
        var queuedForProcessing = new Queue<ICatalogueSymbol>();
        queuedForProcessing.Enqueue(catalogue);
        while (queuedForProcessing.TryDequeue(out var item) && !closureItems.Contains(item))
        {
            closureItems.Add(item);
            foreach (var import in item.CatalogueReferences)
            {
                queuedForProcessing.Enqueue(import.Catalogue);
            }
        }
        closureItems.Add(catalogue.Gamesystem);
        // TODO consider filtering out "missing"/error items
        return closureItems.ToImmutableArray();
    }

    internal static void LookupSymbolsInSingleCatalogue(
        ICatalogueSymbol catalogue,
        LookupResult result,
        string symbolId,
        LookupOptions options,
        Binder originalBinder,
        bool diagnose)
    {
        if (options.CanConsiderResourceDefinitions())
        {
            originalBinder.CheckViability(result, catalogue.ResourceDefinitions, symbolId, options, diagnose);
        }
        if (options.CanConsiderRootEntries())
        {
            if (options.CanConsiderContainerEntries())
            {
                originalBinder.CheckViability(result, catalogue.RootContainerEntries, symbolId, options, diagnose);
            }
            if (options.CanConsiderResourceEntries())
            {
                originalBinder.CheckViability(result, catalogue.RootResourceEntries, symbolId, options, diagnose);
            }
        }
        if (options.CanConsiderSharedEntries())
        {
            if (options.CanConsiderContainerEntries())
            {
                originalBinder.CheckViability(result, catalogue.SharedSelectionEntryContainers, symbolId, options, diagnose);
            }
            if (options.CanConsiderResourceEntries())
            {
                originalBinder.CheckViability(result, catalogue.SharedResourceEntries, symbolId, options, diagnose);
            }
        }
    }

    internal static void LookupSymbolInQualifyingEntryContainer(
        ISelectionEntryContainerSymbol qualifier,
        LookupResult result,
        string symbolId,
        LookupOptions options,
        Binder originalBinder,
        bool diagnose,
        ImmutableArray<ICatalogueSymbol> rootClosure)
    {
        var unreferenced = qualifier.ContainingModule is not ICatalogueSymbol containingCatalogue || !rootClosure.Contains(containingCatalogue);
        if (unreferenced && !diagnose)
        {
            return;
        }
        var queue = ImmutableQueue.Create(qualifier);
        while (!queue.IsEmpty && !result.IsMultiViable)
        {
            queue = queue.Dequeue(out var symbolToCheck);
            var singleResult = originalBinder.CheckViability(symbolToCheck, symbolId, options, diagnose);
            if (singleResult is not { Kind: LookupResultKind.Empty })
            {
                singleResult = !unreferenced ? singleResult : singleResult with
                {
                    Kind = LookupResultKind.Unreferenced,
                };
                result.MergeEqual(singleResult);
            }
            if (!result.IsMultiViable)
            {
                foreach (var child in symbolToCheck.ChildSelectionEntries)
                    queue = queue.Enqueue(child);
            }
        }
    }

    private static bool IsRootEntry(ISymbol symbol) => symbol.ContainingModule is ICatalogueSymbol { } catalogue && symbol.Kind switch
    {
        SymbolKind.ContainerEntry => catalogue.RootContainerEntries.Contains(symbol),
        SymbolKind.Resource => catalogue.RootResourceEntries.Contains(symbol),
        _ => false,
    };

    private static bool IsSharedEntry(ISymbol symbol) => symbol.ContainingModule is ICatalogueSymbol { } catalogue && symbol.Kind switch
    {
        SymbolKind.ContainerEntry => catalogue.SharedSelectionEntryContainers.Contains(symbol),
        SymbolKind.Resource => catalogue.SharedResourceEntries.Contains(symbol),
        _ => false,
    };

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method is WIP")]
    internal ISymbol ResultSymbol(
        LookupResult result,
        string symbolId,
        SourceNode where,
        BindingDiagnosticBag diagnostics,
        out bool wasError,
        LookupOptions options = LookupOptions.Default)
    {
        Debug.Assert(where is not null);
        Debug.Assert(diagnostics is not null);
        var symbols = result.Symbols;
        wasError = false;
        if (result.IsMultiViable)
        {
            if (result.SingleSymbolOrDefault is { } singleSymbol)
            {
                return singleSymbol;
            }
            else
            {
                // TODO sort by "bestness"?
                // TODO multi-result analysis
                // TODO report possible warnings or errors
                // TODO candidate-containing result
                var diag = diagnostics.Add(ErrorCode.ERR_MultipleViableBindingCandidates, where.GetLocation(), result.Symbols.ToImmutableArray(), symbolId);
                return new ErrorSymbols.ErrorSymbolBase()
                {
                    ErrorInfo = diag,
                };
            }
        }
        wasError = true;
        if (result.Kind is LookupResultKind.Empty)
        {
            // TODO diagnostics, specific type?
            var diag = diagnostics.Add(ErrorCode.ERR_NoBindingCandidates, where.GetLocation(), symbolId);
            return new ErrorSymbols.ErrorSymbolBase()
            {
                ErrorInfo = diag,
            };
        }

        Debug.Assert(symbols.Count > 0);

        DiagnosticInfo? errorDiag;
        if (result.Error is { } error)
        {
            errorDiag = error;
            diagnostics.Add(new WhamDiagnostic(error, where.GetLocation()));
        }
        else
        {
            errorDiag = diagnostics.Add(ErrorCode.ERR_UnviableBindingCandidates, where.GetLocation(), result.Symbols.ToImmutableArray(), symbolId);
        }

        if (result.SingleSymbolOrDefault is { } singleUnviable)
        {
            // single non-viable, error already reported - just return
            return singleUnviable;
        }
        else
        {
            // multiple - package up
            // TODO candidate-containing result
            return new ErrorSymbols.ErrorSymbolBase()
            {
                ErrorInfo = errorDiag,
            };
        }
    }

    internal virtual void LookupSymbolsInSingleBinder(
        LookupResult result,
        string symbolId,
        LookupOptions options,
        Binder originalBinder,
        bool diagnose,
        ISymbol? qualifier)
    {
    }

    private Binder? LookupSymbolsWithDelayedDiagnosing(
        LookupResult result,
        string symbolId,
        LookupOptions options = LookupOptions.Default,
        ISymbol? qualifier = null)
    {
        var binder = LookupSymbolsWithScopeTraversal(result, symbolId, options, diagnose: false, qualifier);
        Debug.Assert(binder is not null || result.IsClear);

        if (result.Kind is not LookupResultKind.Empty and not LookupResultKind.Viable)
        {
            result.Clear();
            // retry to get diagnosis
            var otherBinder = LookupSymbolsWithScopeTraversal(result, symbolId, options, diagnose: true, qualifier);
            Debug.Assert(binder == otherBinder);
        }
        Debug.Assert(result.IsMultiViable || result.IsClear || result.Error is not null);
        return binder;
    }

    private Binder? LookupSymbolsWithScopeTraversal(
        LookupResult result,
        string symbolId,
        LookupOptions options,
        bool diagnose,
        ISymbol? qualifier)
    {
        Debug.Assert(result.IsClear);
        Binder? binder = null;
        // traverse scopes from this to the next and next
        // until there's no next, or a result is viable.
        for (var scope = this; scope != null && !result.IsMultiViable; scope = scope.Next)
        {
            if (binder is null)
            {
                // we didn't yet get any results
                scope.LookupSymbolsInSingleBinder(result, symbolId, options, this, diagnose, qualifier);
                if (!result.IsClear)
                {
                    // first binder to set non-empty-result, save it to return
                    binder = scope;
                }
            }
            else
            {
                // we already set some non-empty result, so we create a temporary to merge with it
                var tmpResult = LookupResult.GetInstance();
                scope.LookupSymbolsInSingleBinder(tmpResult, symbolId, options, this, diagnose, qualifier);
                result.MergeEqual(tmpResult);
                tmpResult.Free();
            }
        }
        return binder;
    }
}
