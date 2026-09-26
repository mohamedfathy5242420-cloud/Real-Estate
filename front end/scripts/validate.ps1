param([string]$RootPath = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$repoPath = (Resolve-Path -LiteralPath $RootPath).Path
$required = @(
    'AGENTS.md', 'README.md',
    'docs/PROJECT_CONTEXT.md', 'docs/DECISIONS.md', 'docs/RISKS.md',
    'docs/CURRENT_STATE.md', 'docs/WORKFLOW.md', 'docs/VALIDATION.md',
    'docs/ARCHITECTURE.md', 'docs/ROADMAP.md', 'docs/API_CONTRACT.md',
    'handoffs/CURRENT_HANDOFF.md',
    'planning/SESSION_START.md',
    'planning/phase-00/README.md', 'planning/phase-00/steps/S01.md',
    'planning/phase-00/evidence.md', 'planning/phase-00/PHASE_REVIEW.md',
    'planning/phase-01/README.md', 'planning/phase-01/PHASE_REVIEW.md',
    'planning/phase-02/README.md', 'planning/phase-02/steps/S01.md',
    'planning/phase-02/evidence-S01.md',
    'planning/phase-00/steps/S02.md', 'planning/phase-00/evidence-S02.md',
    'docs/business/README.md', 'docs/business/SOURCES.json', 'docs/business/TRACEABILITY.json',
    'docs/business/SCREENS_AND_FLOWS.md', 'docs/business/UI_ACCEPTANCE.md',
    'docs/business/shared/BUSINESS_OVERVIEW.md', 'docs/business/shared/ACTORS_AND_PERMISSIONS.md',
    'docs/business/shared/BUSINESS_RULES.md', 'docs/business/shared/USE_CASES.md',
    'docs/business/shared/STATE_TRANSITIONS.md', 'docs/business/shared/ACCEPTANCE_SCENARIOS.md',
    'docs/business/shared/MVP_SCOPE.md',
    'planning/templates/PHASE.md', 'planning/templates/STEP.md',
    'planning/templates/EVIDENCE.md', 'planning/templates/PHASE_REVIEW.md',
    'planning/templates/HANDOFF.md',
    'app/README.md', 'app/package.json', 'app/pnpm-lock.yaml', 'app/pnpm-workspace.yaml',
    'app/angular.json', 'app/tsconfig.json', 'app/src/index.html', 'app/src/styles.css',
    'app/src/app/app.ts', 'app/src/app/app.html', 'app/src/app/app.routes.ts',
    'app/src/app/app.routes.server.ts',
    'app/src/app/pages/foundation-page/foundation-page.ts'
)
foreach ($relative in $required) {
    $filePath = Join-Path $repoPath $relative
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        throw "Missing required workflow file: $relative"
    }
    if ([string]::IsNullOrWhiteSpace((Get-Content -Encoding UTF8 -LiteralPath $filePath -Raw))) {
        throw "Empty required workflow file: $relative"
    }
}
function Read-Field([string]$Body, [string]$Name) {
    $match = [regex]::Match($Body, "(?m)^" + [regex]::Escape($Name) + ":\s*([^\r\n]+)")
    if (-not $match.Success) { throw "Missing field: $Name" }
    return $match.Groups[1].Value.Trim()
}
$state = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $repoPath 'docs/CURRENT_STATE.md') -Raw
$handoff = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $repoPath 'handoffs/CURRENT_HANDOFF.md') -Raw
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
$stepBody = Get-Content -Encoding UTF8 -LiteralPath $activePath -Raw
if ((Read-Field $stepBody 'Status') -ne $status) { throw 'Active step status differs from current state.' }
$stepId = Read-Field $state 'Step'
if (-not $stepBody.StartsWith("# $stepId ")) { throw 'Active step identifier mismatch.' }
$activeCount = @(Get-ChildItem -LiteralPath (Join-Path $repoPath 'planning') -Filter '*.md' -Recurse |
    Where-Object { $_.Directory.Name -eq 'steps' } |
    Where-Object { (Get-Content -Encoding UTF8 -LiteralPath $_.FullName -Raw) -match '(?m)^Status: InProgress\s*$' }).Count
if ($activeCount -gt 1) { throw 'More than one step is InProgress.' }
if (Test-Path -LiteralPath (Join-Path $repoPath 'app/.git')) { throw 'Angular app must not contain a nested Git repository.' }
Write-Host "PASS: $($required.Count) required workflow files are present and nonempty."
Write-Host 'PASS: current state, handoff and active step are consistent.'
Write-Host 'PASS: at most one implementation step is InProgress.'
Write-Host 'Scope: document structure only. Manual review and application tests are NOT implied.'

function Read-Ids([string]$Relative, [string]$Prefix) {
    $body = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $repoPath $Relative) -Raw
    $ids = @([regex]::Matches($body, "(?m)^## ($Prefix-\d+) ") | ForEach-Object { $_.Groups[1].Value })
    if ($ids.Count -eq 0 -or @($ids | Select-Object -Unique).Count -ne $ids.Count) { throw "Missing/duplicate definitions: $Relative" }
    return $ids
}
$definitions = @{
    rules = @(Read-Ids 'docs/business/shared/BUSINESS_RULES.md' 'BR')
    useCase = @(Read-Ids 'docs/business/shared/USE_CASES.md' 'UC')
    screens = @(Read-Ids 'docs/business/SCREENS_AND_FLOWS.md' 'SC')
    acceptance = @(Read-Ids 'docs/business/UI_ACCEPTANCE.md' 'FAC')
}
$trace = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $repoPath 'docs/business/TRACEABILITY.json') -Raw | ConvertFrom-Json
if ($trace.schemaVersion -ne 1 -or @($trace.entries).Count -eq 0) { throw 'Invalid traceability schema.' }
$roadmapBody = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $repoPath 'docs/ROADMAP.md') -Raw
$phases = @([regex]::Matches($roadmapBody, '\| (F\d+) \|') | ForEach-Object { $_.Groups[1].Value })
$seen = @{ rules=@(); useCase=@(); screens=@(); acceptance=@() }
foreach ($entry in $trace.entries) {
    if ($entry.useCase -in $seen.useCase) { throw "Duplicate UC: $($entry.useCase)" }
    foreach ($field in $definitions.Keys) {
        if (@($entry.$field).Count -eq 0) { throw "Empty trace field: $field" }
        foreach ($id in @($entry.$field)) {
            if ($id -notin $definitions[$field]) { throw "Unknown $field reference: $id" }
            $seen[$field] += $id
        }
    }
    if (@($entry.phases).Count -eq 0) { throw 'Missing implementation phase.' }
    foreach ($phase in $entry.phases) { if ($phase -notin $phases) { throw "Unknown phase: $phase" } }
    if ($entry.implementation -notin @('NotStarted','InProgress','Verified')) { throw 'Invalid implementation status.' }
    if ($entry.implementation -eq 'Verified' -and @($entry.testEvidence).Count -eq 0) { throw 'Verified requires actual test evidence.' }
}
foreach ($field in $definitions.Keys) {
    foreach ($id in $definitions[$field]) { if ($id -notin $seen[$field]) { throw "Unmapped $field reference: $id" } }
}
$activeFields = @{ BusinessRefs='rules'; UseCaseRefs='useCase'; ScreenRefs='screens'; AcceptanceRefs='acceptance' }
foreach ($field in $activeFields.Keys) {
    $refs = Read-Field $stepBody $field
    if ($refs -match '^N/A\s*[-:]\s*\S') { continue }
    foreach ($id in ($refs -split ',\s*')) {
        if ($id.Trim() -notin $definitions[$activeFields[$field]]) { throw "Unknown active-step reference: $id" }
    }
}
Write-Host "PASS: frontend Business linkage - $($definitions.rules.Count) rules, $($definitions.useCase.Count) use cases, $($definitions.screens.Count) screens, $($definitions.acceptance.Count) UI scenarios."

$manifest = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $repoPath 'docs/business/SOURCES.json') -Raw | ConvertFrom-Json
if ($manifest.version -ne 1 -or @($manifest.files).Count -eq 0) { throw 'Invalid source manifest.' }
$sourceRoot = [IO.Path]::GetFullPath((Join-Path $repoPath $manifest.canonicalRootRelative))
$checked = 0
$missing = 0
foreach ($file in $manifest.files) {
    $snapshot = Join-Path $repoPath $file.snapshot
    if (-not (Test-Path -LiteralPath $snapshot -PathType Leaf)) { throw "Missing business snapshot: $($file.snapshot)" }
    $source = Join-Path $sourceRoot $file.source
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { $missing++; continue }
    $originalText = (Get-Content -Encoding UTF8 -LiteralPath $source -Raw).Replace("`r`n", "`n").TrimEnd()
    $snapshotText = (Get-Content -Encoding UTF8 -LiteralPath $snapshot -Raw).Replace("`r`n", "`n").TrimEnd()
    if ($originalText -cne $snapshotText) { throw "Business snapshot drift: $($file.source). Review source changes before syncing." }
    $checked++
}
if ($missing -gt 0) { Write-Warning "Business synchronization NOT VERIFIED for $missing unavailable source files; $checked available files matched." }
else { Write-Host "PASS: $checked Business snapshots match canonical source." }
