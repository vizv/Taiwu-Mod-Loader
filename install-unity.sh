#!/bin/sh
set -ex

UNITY_VERSION="$(cat Unity.version)"
[ -d "Unity" ] && {
  CURRENT_VERSION="$(cat Unity/version)"
  [ "${CURRENT_VERSION}" = "${UNITY_VERSION}" ] && exit
  rm -rf Unity
}

curl -o /tmp/UnitySetup64.exe "https://download.unity3d.com/download_unity/38bd7dec5000/Windows64EditorInstaller/UnitySetup64-${UNITY_VERSION}f1.exe"
7z -oUnity x /tmp/UnitySetup64.exe

mkdir -p Unity/lib
cp Unity/Editor/Data/Managed/UnityEngine/* Unity/lib
cp Unity/Editor/Data/UnityExtensions/Unity/GUISystem/Standalone/* Unity/lib
rm -rf Unity/{Editor,\$PLUGINSDIR}

echo "${UNITY_VERSION}" > Unity/version
