param([string]$RootPath = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$PSDefaultParameterValues['Get-Content:Encoding'] = 'UTF8'
$repoPath = (Resolve-Path -LiteralPath $RootPath).Path
$required = @(
    'AGENTS.md', 'README.md',
    'docs/PROJECT_CONTEXT.md', 'docs/DECISIONS.md', 'docs/RISKS.md',
    'docs/CURRENT_STATE.md', 'docs/WORKFLOW.md', 'docs/VALIDATION.md',
    'docs/ARCHITECTURE.md', 'docs/ROADMAP.md',
    'handoffs/CURRENT_HANDOFF.md',
    'planning/SESSION_START.md',
    'planning/phase-00/README.md', 'planning/phase-00/steps/S01.md',
    'planning/phase-00/evidence.md', 'planning/phase-00/PHASE_REVIEW.md',
    'planning/phase-01/README.md', 'docs/MVP_SCOPE.md',
    'planning/phase-01/steps/S01.md', 'planning/phase-01/evidence-S01.md',
    'planning/phase-01/steps/S02.md', 'planning/phase-01/evidence-S02.md',
    'planning/phase-01/steps/S03.md', 'planning/phase-01/evidence-S03.md',
    'planning/phase-01/steps/S04.md', 'planning/phase-01/evidence-S04.md',
    'planning/phase-01/PHASE_REVIEW.md',
    'planning/phase-02/README.md', 'planning/phase-02/steps/S01.md', 'planning/phase-02/evidence-S01.md',
    'planning/phase-02/steps/S02.md', 'planning/phase-02/evidence-S02.md',
    'planning/phase-02/steps/S03.md', 'planning/phase-02/evidence-S03.md',
    'global.json', 'BrokerHub.sln', 'Directory.Build.props', '.config/dotnet-tools.json',
    'Directory.Packages.props', '.editorconfig',
    'src/BrokerHub.Domain/BrokerHub.Domain.csproj',
    'src/BrokerHub.Application/BrokerHub.Application.csproj',
    'src/BrokerHub.Infrastructure/BrokerHub.Infrastructure.csproj',
    'src/BrokerHub.Api/BrokerHub.Api.csproj',
    'src/BrokerHub.Application/DependencyInjection.cs',
    'src/BrokerHub.Application/Abstractions/Identity/ICurrentUser.cs',
    'src/BrokerHub.Application/Authorization/AppRoles.cs',
    'src/BrokerHub.Infrastructure/DependencyInjection.cs',
    'src/BrokerHub.Infrastructure/Identity/ApplicationUser.cs',
    'src/BrokerHub.Infrastructure/Persistence/PersistenceDependencyInjection.cs',
    'src/BrokerHub.Infrastructure/Persistence/ApplicationDbContext.cs',
    'src/BrokerHub.Infrastructure/Persistence/Migrations/20260926133217_InitialIdentity.cs',
    'src/BrokerHub.Infrastructure/Persistence/Migrations/20260926133217_InitialIdentity.Designer.cs',
    'src/BrokerHub.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs',
    'src/BrokerHub.Infrastructure/Identity/IdentityDependencyInjection.cs',
    'src/BrokerHub.Infrastructure/Messaging/MessagingDependencyInjection.cs',
    'src/BrokerHub.Infrastructure/Caching/CachingDependencyInjection.cs',
    'src/BrokerHub.Infrastructure/Time/TimeDependencyInjection.cs',
    'src/BrokerHub.Api/Configuration/BrokerHubOptions.cs',
    'src/BrokerHub.Api/Authorization/AuthorizationPolicies.cs',
    'src/BrokerHub.Api/Authorization/AuthorizationServiceCollectionExtensions.cs',
    'src/BrokerHub.Api/Identity/HttpCurrentUser.cs',
    'src/BrokerHub.Api/Program.cs', 'src/BrokerHub.Api/appsettings.json',
    'src/BrokerHub.Api/appsettings.Development.json',
    'tests/BrokerHub.Domain.Tests/BrokerHub.Domain.Tests.csproj',
    'tests/BrokerHub.Application.Tests/BrokerHub.Application.Tests.csproj',
    'tests/BrokerHub.Architecture.Tests/BrokerHub.Architecture.Tests.csproj',
    'tests/BrokerHub.IntegrationTests/BrokerHub.IntegrationTests.csproj',
    'handoffs/AGENT_ASSIGNMENT_P01_S03.md',
    'docs/business/README.md', 'docs/business/BUSINESS_OVERVIEW.md',
    'docs/business/ACTORS_AND_PERMISSIONS.md', 'docs/business/BUSINESS_RULES.md',
    'docs/business/USE_CASES.md', 'docs/business/STATE_TRANSITIONS.md',
    'docs/business/ACCEPTANCE_SCENARIOS.md', 'docs/business/TRACEABILITY.json',
    'docs/business/CONCURRENCY_AND_IDEMPOTENCY.md', 'docs/business/DECISION_PACKAGE_P01_S03.md',
    'docs/business/DOMAIN_MODEL.md', 'docs/business/ERD.md',
    'docs/business/CONSTRAINTS.md', 'docs/business/P02_TECHNICAL_FOUNDATION.md',
    'planning/templates/USE_CASE.md',
    'planning/templates/PHASE.md', 'planning/templates/STEP.md',
    'planning/templates/EVIDENCE.md', 'planning/templates/PHASE_REVIEW.md',
    'planning/templates/HANDOFF.md'
)
foreach ($relative in $required) {
    $filePath = Join-Path $repoPath $relative
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        throw "Missing required workflow file: $relative"
    }
    if ([string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $filePath -Raw))) {
        throw "Empty required workflow file: $relative"
    }
}
function Read-Field([string]$Body, [string]$Name) {
    $match = [regex]::Match($Body, "(?m)^" + [regex]::Escape($Name) + ":\s*([^\r\n]+)")
    if (-not $match.Success) { throw "Missing field: $Name" }
    return $match.Groups[1].Value.Trim()
}
$state = Get-Content -LiteralPath (Join-Path $repoPath 'docs/CURRENT_STATE.md') -Raw
$handoff = Get-Content -LiteralPath (Join-Path $repoPath 'handoffs/CURRENT_HANDOFF.md') -Raw
foreach ($field in @('Phase', 'Step', 'Status')) {
    if ((Read-Field $state $field) -ne (Read-Field $handoff $field)) {
        throw "State/handoff mismatch: $field"
    }
}
$status = Read-Field $state 'Status'
if ($status -notin @('Draft','Ready','InProgress','InReview','Done','Blocked')) {
    throw "Invalid step status: $status"
}
$activeRelative = Read-Field $state 'ActiveStepFile'
$activePath = [IO.Path]::GetFullPath((Join-Path $repoPath $activeRelative))
$rootPrefix = $repoPath.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $activePath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Active step must be inside the repository.'
}
if (-not (Test-Path -LiteralPath $activePath -PathType Leaf)) { throw 'Active step file does not exist.' }
$stepBody = Get-Content -LiteralPath $activePath -Raw
if ((Read-Field $stepBody 'Status') -ne $status) { throw 'Active step status differs from current state.' }
$stepId = Read-Field $state 'Step'
if (-not $stepBody.StartsWith("# $stepId ")) { throw 'Active step identifier mismatch.' }
$activeCount = @(Get-ChildItem -LiteralPath (Join-Path $repoPath 'planning') -Filter '*.md' -Recurse |
    Where-Object { $_.Directory.Name -eq 'steps' } |
    Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match '(?m)^Status: InProgress\s*$' }).Count
if ($activeCount -gt 1) { throw 'More than one step is InProgress.' }
Write-Host "PASS: $($required.Count) required workflow files are present and nonempty."
Write-Host 'PASS: current state, handoff and active step are consistent.'
Write-Host 'PASS: at most one implementation step is InProgress.'
Write-Host 'Scope: document structure only. Manual review and application tests are NOT implied.'

# Business traceability checks. References prove linkage, not implemented behavior.
function Read-Ids([string]$Relative, [string]$Prefix) {
    $body = Get-Content -LiteralPath (Join-Path $repoPath $Relative) -Raw
    $ids = @([regex]::Matches($body, "(?m)^## ($Prefix-\d+) ") | ForEach-Object { $_.Groups[1].Value })
    if ($ids.Count -eq 0) { throw "No definitions in $Relative" }
    if (@($ids | Select-Object -Unique).Count -ne $ids.Count) { throw "Duplicate definitions in $Relative" }
    return $ids
}
$ruleIds = @(Read-Ids 'docs/business/BUSINESS_RULES.md' 'BR')
$caseIds = @(Read-Ids 'docs/business/USE_CASES.md' 'UC')
$acceptanceIds = @(Read-Ids 'docs/business/ACCEPTANCE_SCENARIOS.md' 'AC')
$trace = Get-Content -LiteralPath (Join-Path $repoPath 'docs/business/TRACEABILITY.json') -Raw | ConvertFrom-Json
if ($trace.schemaVersion -ne 1 -or @($trace.entries).Count -eq 0) { throw 'Invalid traceability schema.' }
$roadmapBody = Get-Content -LiteralPath (Join-Path $repoPath 'docs/ROADMAP.md') -Raw
$phaseIds = @([regex]::Matches($roadmapBody, '\| (P\d+) \|') | ForEach-Object { $_.Groups[1].Value })
$mvpBody = Get-Content -LiteralPath (Join-Path $repoPath 'docs/MVP_SCOPE.md') -Raw
$questionIds = @([regex]::Matches($mvpBody, '\| (Q\d+) \|') | ForEach-Object { $_.Groups[1].Value })
$seenCases = @()
$seenRules = @()
$seenAcceptance = @()
foreach ($entry in $trace.entries) {
    if ($entry.useCase -notin $caseIds -or $entry.useCase -in $seenCases) { throw "Unknown/duplicate UC: $($entry.useCase)" }
    if (@($entry.rules).Count -eq 0 -or @($entry.acceptance).Count -eq 0 -or @($entry.phases).Count -eq 0) { throw 'Incomplete traceability entry.' }
    foreach ($id in $entry.rules) { if ($id -notin $ruleIds) { throw "Unknown rule: $id" } }
    foreach ($id in $entry.acceptance) { if ($id -notin $acceptanceIds) { throw "Unknown acceptance: $id" } }
    foreach ($id in $entry.phases) { if ($id -notin $phaseIds) { throw "Unknown phase: $id" } }
    foreach ($id in $entry.openQuestions) { if ($id -notin $questionIds) { throw "Unknown question: $id" } }
    if ($entry.implementation -notin @('NotStarted','InProgress','Verified')) { throw 'Invalid implementation status.' }
    if ($entry.implementation -eq 'Verified' -and @($entry.testEvidence).Count -eq 0) { throw 'Verified requires evidence.' }
    $seenCases += $entry.useCase
    $seenRules += $entry.rules
    $seenAcceptance += $entry.acceptance
}
foreach ($id in $caseIds) { if ($id -notin $seenCases) { throw "Unmapped UC: $id" } }
foreach ($id in $ruleIds) { if ($id -notin $seenRules) { throw "Unmapped rule: $id" } }
foreach ($id in $acceptanceIds) { if ($id -notin $seenAcceptance) { throw "Unmapped acceptance: $id" } }
foreach ($pair in @(@('BusinessRefs', $ruleIds), @('UseCaseRefs', $caseIds), @('AcceptanceRefs', $acceptanceIds))) {
    $refs = Read-Field $stepBody $pair[0]
    if ($refs -match '^N/A\s*[-:]\s*\S') { continue }
    foreach ($id in ($refs -split ',\s*')) {
        if ($id.Trim() -notin $pair[1]) { throw "Unknown active-step reference: $id" }
    }
}
Write-Host "PASS: Business linkage - $($ruleIds.Count) rules, $($caseIds.Count) use cases, $($acceptanceIds.Count) acceptance scenarios."
