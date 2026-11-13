#!/usr/bin/env python3
"""Build a Unity .unitypackage from the NovaOS-FNF source and official FNF assets.

Unity's .unitypackage format is a gzip-compressed tar archive. This builder writes
Unity's per-asset GUID folders, pathname, asset and asset.meta entries so the
result can be imported by Unity's Package Import dialog without running an
asset-fetching script inside the user's project.
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import os
import tarfile
import tempfile
from pathlib import Path
from typing import Iterable

NAMESPACE = "NovaOS-FNF-UnityPackage-v1"
REPO_ROOT = Path(__file__).resolve().parents[2]
ASSET_REPO = Path(os.environ.get("FNF_ASSET_REPO", "build/funkin.assets"))
OUTPUT = Path(os.environ.get("PACKAGE_OUTPUT", "dist/NovaOS-FNF.unitypackage"))
UNITY_ROOT = "Assets/NovaOS/FNF"


def guid_for(path: str) -> str:
    return hashlib.md5((NAMESPACE + ":" + path.replace("\\", "/")).encode("utf-8")).hexdigest()


def meta_for(path: str, is_dir: bool) -> str:
    ext = Path(path).suffix.lower()
    if is_dir:
        importer = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: "
    elif ext == ".cs":
        importer = "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: "
    elif ext in (".ogg", ".mp3", ".wav", ".aiff", ".aif"):
        importer = "AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n    quality: 1\n    conversionMode: 0\n  platformSettingOverrides: {}\n  forceToMono: 0\n  normalize: 1\n  preloadAudioData: 0\n  loadInBackground: 0\n  ambisonic: 0\n  3D: 1\n  userData: \n  assetBundleName: \n  assetBundleVariant: "
    elif ext in (".png", ".jpg", ".jpeg", ".tga", ".psd"):
        importer = "TextureImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 12\n  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 1\n    sRGBTexture: 1\n    streamingMipmaps: 0\n    borderMipMap: 0\n    mipMapsPreserveCoverage: 0\n    alphaTestReferenceValue: 0.5\n    mipMapFadeDistanceStart: 1\n    mipMapFadeDistanceEnd: 3\n  bumpmap:\n    convertToNormalMap: 0\n    externalNormalMap: 0\n    heightScale: 0.25\n    normalMapFilter: 0\n  isReadable: 0\n  streamingMipmapsPriority: 0\n  vTOnly: 0\n  ignoreMipmapLimit: 0\n  grayScaleToAlpha: 0\n  generateCubemap: 6\n  cubemapConvolution: 0\n  seamlessCubemap: 0\n  textureFormat: 1\n  maxTextureSize: 2048\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 1\n    aniso: 1\n    mipBias: 0\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n  nPOTScale: 1\n  lightmap: 0\n  compressionQuality: 50\n  spriteMode: 1\n  spriteExtrude: 1\n  spriteMeshType: 1\n  alignment: 0\n  spritePivot: {x: 0.5, y: 0.5}\n  spritePixelsToUnits: 100\n  spriteBorder: {x: 0, y: 0, z: 0, w: 0}\n  spriteGenerateFallbackPhysicsShape: 1\n  alphaUsage: 1\n  alphaIsTransparency: 1\n  spriteTessellationDetail: -1\n  textureType: 8\n  textureShape: 1\n  singleChannelComponent: 0\n  flipbookRows: 1\n  flipbookColumns: 1\n  maxTextureSizeSet: 0\n  compressionQualitySet: 0\n  textureFormatSet: 0\n  ignorePngGamma: 0\n  applyGammaDecoding: 0\n  cookieLightType: 0\n  platformSettings: []\n  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    physicsShape: []\n    bones: []\n    spriteID: \n    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n    nameFileIdTable: {}\n  mipmapLimitGroupName: \n  pSDRemoveMatte: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: "
    elif ext in (".ttf", ".otf"):
        importer = "TrueTypeFontImporter:\n  externalObjects: {}\n  serializedVersion: 4\n  fontSize: 16\n  forceTextureCase: -2\n  characterSpacing: 1\n  characterPadding: 0\n  includeFontData: 1\n  fontNames: []\n  fallbackFontReferences: []\n  customCharacters: \n  fontRenderingMode: 0\n  ascentCalculationMode: 1\n  userData: \n  assetBundleName: \n  assetBundleVariant: "
    else:
        importer = "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: "
    return f"fileFormatVersion: 2\nguid: {guid_for(path)}\n{importer}\n"


def safe_files(root: Path) -> Iterable[Path]:
    for p in sorted(root.rglob("*")):
        if not p.is_file():
            continue
        if ".git" in p.parts or p.name.endswith(".meta"):
            continue
        yield p


def add_entry(tf: tarfile.TarFile, name: str, data: bytes) -> None:
    info = tarfile.TarInfo(name)
    info.size = len(data)
    info.mtime = 0
    info.mode = 0o644
    import io
    tf.addfile(info, io.BytesIO(data))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=REPO_ROOT / "Assets" / "NovaOS" / "FNF")
    parser.add_argument("--official-assets", type=Path, default=ASSET_REPO)
    parser.add_argument("--output", type=Path, default=OUTPUT)
    args = parser.parse_args()

    source = args.source.resolve()
    official = args.official_assets.resolve()
    if not source.exists():
        raise SystemExit(f"NovaOS-FNF source folder not found: {source}")
    if not official.exists():
        raise SystemExit(f"Official asset checkout not found: {official}")

    # Preserve the project's existing FNF source paths, then place the official
    # asset repository under one predictable root inside the Unity project.
    files: list[tuple[Path, str]] = []
    dirs: set[str] = {UNITY_ROOT}
    for p in source.rglob("*"):
        if p.is_dir():
            continue
        if p.name.endswith(".meta"):
            continue
        rel = p.relative_to(source).as_posix()
        dest = f"{UNITY_ROOT}/{rel}"
        files.append((p, dest))
        parent = Path(dest).parent
        while str(parent).startswith("Assets"):
            dirs.add(parent.as_posix())
            if parent.as_posix() == "Assets":
                break
            parent = parent.parent

    official_root = f"{UNITY_ROOT}/OfficialAssets"
    dirs.add(official_root)
    for p in safe_files(official):
        rel = p.relative_to(official).as_posix()
        dest = f"{official_root}/{rel}"
        files.append((p, dest))
        parent = Path(dest).parent
        while str(parent).startswith("Assets"):
            dirs.add(parent.as_posix())
            if parent.as_posix() == "Assets":
                break
            parent = parent.parent

    # Add Unity's root folders as folder assets too.
    dirs.update({"Assets", "Assets/NovaOS", UNITY_ROOT})
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(suffix=".tar.gz", delete=False) as tmp:
        temp_name = tmp.name
    try:
        with gzip.open(temp_name, "wb", compresslevel=6) as gz:
            with tarfile.open(fileobj=gz, mode="w") as tf:
                for d in sorted(dirs, key=lambda x: (x.count("/"), x)):
                    g = guid_for(d)
                    add_entry(tf, f"{g}/pathname", d.encode("utf-8"))
                    add_entry(tf, f"{g}/asset", b"")
                    add_entry(tf, f"{g}/asset.meta", meta_for(d, True).encode("utf-8"))
                for p, dest in files:
                    g = guid_for(dest)
                    add_entry(tf, f"{g}/pathname", dest.encode("utf-8"))
                    add_entry(tf, f"{g}/asset", p.read_bytes())
                    add_entry(tf, f"{g}/asset.meta", meta_for(dest, False).encode("utf-8"))
        Path(temp_name).replace(args.output)
    finally:
        if Path(temp_name).exists():
            Path(temp_name).unlink()
    print(f"Created {args.output} ({args.output.stat().st_size:,} bytes)")
    print(f"Included {len(files):,} files and {len(dirs):,} folders.")


if __name__ == "__main__":
    main()
