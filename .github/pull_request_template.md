## Description

<!-- Quoi et pourquoi. Lier le ticket le cas échéant. -->

## Type de changement

- [ ] `feat` — nouvelle fonctionnalité
- [ ] `fix` — correction de bug
- [ ] `refactor` — refactorisation sans changement fonctionnel
- [ ] `docs` — documentation
- [ ] `test` — tests uniquement
- [ ] `chore` / `ci` — dépendances, configuration, intégration continue

## Migration EF Core

- [ ] Aucune migration
- [ ] Une migration : `NomDeLaMigration` (branche à jour avec `main`, snapshot sans conflit, livrée par `efbundle`)

## Checklist

- [ ] Branche nommée `<type>/<description>` et à jour avec `main`
- [ ] Commits au format Conventional Commits
- [ ] Tests ajoutés ou mis à jour ; `dotnet test erpWpf.sln` vert
- [ ] `dotnet format erpWpf.sln --verify-no-changes` sans écart
- [ ] Principes SOLID respectés (service par cas d'usage, extension par interfaces, dépendances injectées)
- [ ] Aucun anti-pattern STUPID (état statique, `new` de service, `DateTime.Now`, optimisation non mesurée, abréviations, duplication)
- [ ] ViewModels sans logique métier ni accès aux données, code-behind limité à `InitializeComponent()` ; services dépendant de `IAppDbContext`, jamais de `AppDbContext`
- [ ] Nouvelles pages déclarées avec leur permission (`AjouterPage<…>(…, Permissions.…)`) et commandes d'écriture protégées (`CanExecute`)
- [ ] Écrans vérifiés dans l'application (`dotnet run --project src/erpWpf.Wpf`)
- [ ] CI verte
