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
    'src/MeteredGateEntity.cs', 'src/MeteredGateIds.cs',
    'src/MeteredGateInspector.cs', 'src/MeteredGateMod.cs',
    'src/MeteredGateProto.cs', 'src/MeteredGateSettings.cs',
    'AUDIT_REPORT.md', 'RELEASING.md', 'TEST_PLAN.md',
    'COIHUB_DESCRIPTION.md', 'COIHUB_CHANGELOG_0.3.0.txt',
    'GITHUB_RELEASE_NOTES.md', 'docs/设计说明.md',
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
check(manifest.get('version') == '0.3.0', 'manifest version must be 0.3.0')
check(manifest.get('primary_dlls') == ['MeteredGate.dll'], 'manifest must list only MeteredGate.dll')
check(manifest.get('primary_mod_class_name') == 'MeteredGate.MeteredGateMod', 'wrong primary mod class')

text_files = [p for p in root.rglob('*') if p.is_file() and 'tools' not in p.parts and p.suffix.lower() in {'.cs', '.csproj', '.json', '.md', '.txt', '.bash'}]
all_text = '\n'.join(p.read_text(encoding='utf-8') for p in text_files)
source_text = '\n'.join(p.read_text(encoding='utf-8') for p in (root / 'src').glob('*.cs'))
project_text = (root / 'MeteredGate.csproj').read_text(encoding='utf-8')
entity_text = (root / 'src/MeteredGateEntity.cs').read_text(encoding='utf-8')
proto_text = (root / 'src/MeteredGateProto.cs').read_text(encoding='utf-8')
ids_text = (root / 'src/MeteredGateIds.cs').read_text(encoding='utf-8')
build_text = (root / 'build.bash').read_text(encoding='utf-8')

check('namespace MeteredGate' in source_text, 'formal namespace missing')
check('MeteredGateTest' not in all_text, 'test identity remains in source tree')
check('MeteredGateTest_Entity' not in all_text, 'test prototype id remains')
check('new StaticEntityId("MeteredGate_Entity")' in ids_text, 'formal prototype id missing')
check('<AssemblyName>MeteredGate</AssemblyName>' in project_text, 'assembly name mismatch')
check('<RootNamespace>MeteredGate</RootNamespace>' in project_text, 'root namespace mismatch')
check('<Version>0.3.0</Version>' in project_text, 'project version mismatch')
check('Lib.Harmony' not in project_text, 'Harmony package reference remains')
check('0Harmony.dll' not in manifest.get('primary_dlls', []), 'Harmony DLL remains in manifest')
check(not (root / 'src/MeteredGateHeightPolicy.cs').exists(), 'old height policy file remains')
check(':\n\t\tZipperProto' not in proto_text and ': ZipperProto' not in proto_text, 'custom proto still derives from ZipperProto')
check(':\n\t\tMiniZipperProto' not in proto_text and ': MiniZipperProto' not in proto_text, 'custom proto still derives from MiniZipperProto')
check('LayoutEntityProto,' in proto_text, 'custom proto must derive from LayoutEntityProto')
check('Electricity.FromKw(20)' in (root / 'src/MeteredGateData.cs').read_text(encoding='utf-8'), '20 kW prototype value missing')
check('IElectricityConsumingEntity' in entity_text, 'electricity consuming interface missing')
check('private const int SaveVersion = 2;' in entity_text, 'save format v2 missing')
check('case 1:' in entity_text and 'RegisterInitAfterLoad' in entity_text, 'v1 save migration missing')
check('InitPriority.Lowest' in entity_text, 'v1 migration must run after graph load')
check('MeteredGate.CycleSeconds' in entity_text and 'MeteredGate.ItemsPerCycle' in entity_text, 'formal clone keys missing')
check('public new static MeteredGateConfigCmd Deserialize' in (root / 'src/MeteredGateCommands.cs').read_text(encoding='utf-8'), 'CS0108 suppression missing')
check('MeteredGate-0.3.0.zip' in build_text, 'CoI Hub zip output missing')
check('0Harmony.dll 残留' in build_text, 'old Harmony cleanup warning missing')
check('path.relative_to(root)' in build_text, 'Python ZIP fallback must preserve MeteredGate/ root')
check('sourceConnector.Layout' in proto_text, 'connector layout reuse missing')
check('sourceConnector.Graphics' in proto_text, 'connector graphics reuse missing')
check('sourceBalancer' not in source_text, 'Flat Balancer dependency remains')
check('GetZipperIdFor' not in source_text, 'Balancer prototype lookup remains')
check('workers: 0' in (root / 'src/MeteredGateData.cs').read_text(encoding='utf-8'), 'workers must be explicitly zero')
check('MaintenanceCosts.Empty' in (root / 'src/MeteredGateData.cs').read_text(encoding='utf-8'), 'maintenance must be empty')
check('ElectricityConsumerFactory.CreateConsumer(this)' in entity_text, 'official electricity consumer factory missing')
check('writer.WriteGeneric(m_electricityConsumer)' in entity_text, 'v2 consumer serialization missing')
check('reader.ReadGenericAs<IElectricityConsumer>()' in entity_text, 'v2 consumer deserialization missing')

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
