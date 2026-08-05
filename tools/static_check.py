#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1] if len(sys.argv) > 1 else '.').resolve()
errors: list[str] = []
checks = 0


def check(condition: bool, message: str) -> None:
    global checks
    checks += 1
    if not condition:
        errors.append(message)


required = [
    'MeteredGate.csproj', 'manifest.json', 'config.json', 'build.bash',
    'src/MeteredGateCommands.cs', 'src/MeteredGateData.cs',
    'src/MeteredGateEntity.cs', 'src/MeteredGateHeightValidator.cs',
    'src/MeteredGateIds.cs', 'src/MeteredGateInspector.cs',
    'src/MeteredGateMod.cs', 'src/MeteredGateProto.cs',
    'src/MeteredGateSettings.cs', 'AUDIT_REPORT.md', 'RELEASING.md',
    'TEST_PLAN.md', 'COIHUB_DESCRIPTION.md',
    'COIHUB_CHANGELOG_0.3.1.txt', 'COIHUB_UPLOAD_CHECKLIST.md',
    'GITHUB_RELEASE_NOTES.md', 'GITHUB_UPLOAD.md', 'docs/设计说明.md',
]
for name in required:
    check((root / name).is_file(), f'missing required file: {name}')

try:
    manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
except Exception as exc:
    errors.append(f'manifest.json is invalid: {exc}')
    manifest = {}
try:
    json.loads((root / 'config.json').read_text(encoding='utf-8'))
except Exception as exc:
    errors.append(f'config.json is invalid: {exc}')

check(manifest.get('id') == 'MeteredGate', 'manifest id must be MeteredGate')
check(manifest.get('version') == '0.3.1', 'manifest version must be 0.3.1')
check(manifest.get('primary_dlls') == ['MeteredGate.dll'],
      'manifest must list only MeteredGate.dll')
check(manifest.get('primary_mod_class_name') == 'MeteredGate.MeteredGateMod',
      'wrong primary mod class')
check(manifest.get('can_add_to_saved_game') is True,
      'mod must remain addable to saved games')
check(manifest.get('can_remove_from_saved_game') is False,
      'mod must remain non-removable when entities exist')

text_files = [
    p for p in root.rglob('*')
    if p.is_file()
    and 'tools' not in p.parts
    and p.suffix.lower() in {'.cs', '.csproj', '.json', '.md', '.txt', '.bash'}
]
all_text = '\n'.join(p.read_text(encoding='utf-8') for p in text_files)
source_text = '\n'.join(
    p.read_text(encoding='utf-8') for p in (root / 'src').glob('*.cs')
)
project_text = (root / 'MeteredGate.csproj').read_text(encoding='utf-8')
entity_text = (root / 'src/MeteredGateEntity.cs').read_text(encoding='utf-8')
proto_text = (root / 'src/MeteredGateProto.cs').read_text(encoding='utf-8')
ids_text = (root / 'src/MeteredGateIds.cs').read_text(encoding='utf-8')
data_text = (root / 'src/MeteredGateData.cs').read_text(encoding='utf-8')
commands_text = (root / 'src/MeteredGateCommands.cs').read_text(encoding='utf-8')
inspector_text = (root / 'src/MeteredGateInspector.cs').read_text(encoding='utf-8')
mod_text = (root / 'src/MeteredGateMod.cs').read_text(encoding='utf-8')
validator_text = (root / 'src/MeteredGateHeightValidator.cs').read_text(encoding='utf-8')
build_text = (root / 'build.bash').read_text(encoding='utf-8')

# Formal identity and release version.
check('namespace MeteredGate' in source_text, 'formal namespace missing')
check('MeteredGateTest' not in all_text, 'test identity remains in source tree')
check('MeteredGateTest_Entity' not in all_text, 'test prototype id remains')
check('new StaticEntityId("MeteredGate_Entity")' in ids_text,
      'formal prototype id missing')
check('<AssemblyName>MeteredGate</AssemblyName>' in project_text,
      'assembly name mismatch')
check('<RootNamespace>MeteredGate</RootNamespace>' in project_text,
      'root namespace mismatch')
check('<Version>0.3.1</Version>' in project_text, 'project version mismatch')
check('<FileVersion>0.3.1.0</FileVersion>' in project_text,
      'file version mismatch')
check('<AssemblyVersion>0.3.1.0</AssemblyVersion>' in project_text,
      'assembly version mismatch')
check('MeteredGate-0.3.1.zip' in build_text,
      'CoI Hub 0.3.1 zip output missing')
check('正在编译 Metered Gate 0.3.1' in build_text,
      'build banner version mismatch')

# No Harmony/private placer patch architecture.
check('Lib.Harmony' not in project_text, 'Harmony package reference remains')
check('HarmonyLib' not in source_text, 'Harmony namespace remains in source')
check('0Harmony.dll' not in manifest.get('primary_dlls', []),
      'Harmony DLL remains in manifest')
check(not (root / 'src/MeteredGateHeightPolicy.cs').exists(),
      'old height policy file remains')
check('StaticEntityMassPlacer' not in source_text,
      'private placer coupling remains')
check('LayoutEntityPreview' not in source_text,
      'Unity preview coupling remains')

# Proto architecture.
check(': ZipperProto' not in proto_text,
      'custom proto still derives from ZipperProto')
check(': MiniZipperProto' not in proto_text,
      'custom proto still derives from MiniZipperProto')
check('LayoutEntityProto,' in proto_text,
      'custom proto must derive from LayoutEntityProto')
check('sourceConnector.Layout' in proto_text, 'connector layout reuse missing')
check('sourceConnector.Graphics' in proto_text, 'connector graphics reuse missing')
check('sourceBalancer' not in source_text, 'Flat Balancer dependency remains')
check('GetZipperIdFor' not in source_text, 'Balancer prototype lookup remains')
check('workers: 0' in data_text, 'workers must be explicitly zero')
check('MaintenanceCosts.Empty' in data_text, 'maintenance must be empty')
check('Electricity.FromKw(20)' in data_text, '20 kW prototype value missing')

# Public entity-add height validator.
check('IEntityAdditionValidator<LayoutEntityAddRequest>' in validator_text,
      'typed entity addition validator interface missing')
check('EntityValidatorPriority.Default' in validator_text,
      'validator priority missing')
check('request.Proto is MeteredGateProto proto' in validator_text,
      'validator must only target MeteredGateProto')
check('request.Transform.Position' in validator_text,
      'validator must use the requested final transform')
check('m_terrainManager.TerrainArea.ContainsTile(originTile)' in validator_text,
      'terrain bounds guard missing')
check('m_terrainManager[originTile].Height.TilesHeightRounded' in validator_text,
      'rounded terrain-height lookup missing')
check('placementPosition.Height - terrainHeight' in validator_text,
      'relative-height calculation missing')
check('proto.Layout.PlacementHeightRange' in validator_text,
      'connector placement range lookup missing')
check('relativeHeight >= allowedRange.From' in validator_text,
      'lower-bound check missing')
check('relativeHeight <= allowedRange.To' in validator_text,
      'upper-bound check missing')
check('EntityValidationResult.CreateError' in validator_text,
      'out-of-range rejection missing')
check('RegisterDependencies(' in mod_text,
      'dependency registration override missing')
check('RegisterDependency<MeteredGateHeightValidator>()' in mod_text,
      'height validator dependency registration missing')
check('.AsAllInterfaces()' in mod_text,
      'validator must be registered through its public interfaces')
check('base.RegisterDependencies(builder, protosDb, gameWasLoaded);' in mod_text,
      'base dependency registration call missing')

# Electricity, simulation, commands, and save migration.
check('IElectricityConsumingEntity' in entity_text,
      'electricity consuming interface missing')
check('private const int SaveVersion = 2;' in entity_text,
      'save format v2 missing')
check('case 1:' in entity_text and 'RegisterInitAfterLoad' in entity_text,
      'v1 save migration missing')
check('InitPriority.Lowest' in entity_text,
      'v1 migration must run after graph load')
check('ElectricityConsumerFactory.CreateConsumer(this)' in entity_text,
      'official electricity consumer factory missing')
check('writer.WriteGeneric(m_electricityConsumer)' in entity_text,
      'v2 consumer serialization missing')
check('reader.ReadGenericAs<IElectricityConsumer>()' in entity_text,
      'v2 consumer deserialization missing')
check('MeteredGate.CycleSeconds' in entity_text
      and 'MeteredGate.ItemsPerCycle' in entity_text,
      'formal clone keys missing')
check('public new static MeteredGateConfigCmd Deserialize' in commands_text,
      'CS0108 suppression missing')

# Inspector cycle-duration controls.
for label, delta in (("-30 s", -30), ("-1 s", -1), ("+1 s", 1), ("+30 s", 30)):
    check(f'"{label}".AsLoc()' in inspector_text,
          f'missing cycle control label: {label}')
    check(re.search(
        rf'MeteredGateCommandKind\.AdjustCycleSeconds,\s*{delta}\)\)\)\.Compact\(\)',
        inspector_text) is not None,
        f'cycle control uses wrong delta: {label}')
check('"-10 s".AsLoc()' not in inspector_text
      and '"+10 s".AsLoc()' not in inspector_text,
      'obsolete +/-10 second cycle controls remain')

# Distribution safety.
check('0Harmony.dll 残留' in build_text,
      'old Harmony cleanup warning missing')
check("path.relative_to(root)" in build_text,
      'Python ZIP fallback must preserve MeteredGate/ root')
check('THIRD_PARTY_NOTICES.md' in build_text,
      'third-party notices missing from distribution')

# No generated artifacts should be shipped in source archives.
for generated in ('bin', 'obj', 'dist'):
    check(not (root / generated).exists(),
          f'generated directory must not be in source tree: {generated}')

# Lightweight lexical balance check that ignores strings and comments.
def strip_cs(code: str) -> str:
    code = re.sub(r'@"(?:""|[^"])*"', '""', code, flags=re.S)
    code = re.sub(r'"(?:\\.|[^"\\])*"', '""', code, flags=re.S)
    code = re.sub(r"'(?:\\.|[^'\\])'", "''", code, flags=re.S)
    code = re.sub(r'/\*.*?\*/', '', code, flags=re.S)
    code = re.sub(r'//[^\n]*', '', code)
    return code

for path in (root / 'src').glob('*.cs'):
    clean = strip_cs(path.read_text(encoding='utf-8'))
    for left, right in [('(', ')'), ('[', ']'), ('{', '}')]:
        depth = 0
        for ch in clean:
            if ch == left:
                depth += 1
            elif ch == right:
                depth -= 1
                if depth < 0:
                    break
        check(depth == 0, f'unbalanced {left}{right} in {path.name}')

if errors:
    print(f'STATIC CHECK FAILED: {len(errors)} issue(s), {checks} checks')
    for item in errors:
        print(f'  - {item}')
    raise SystemExit(1)

print(f'STATIC CHECK PASSED: {checks} checks')
