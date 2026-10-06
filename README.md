<h3 align="center">Tech Stack</h3>

<p align="center">
  <img src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/unity/unity-original.svg" height="40" alt="Unity" title="Unity" />
  <img src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/csharp/csharp-original.svg" height="40" alt="C#" title="C#" />
</p>

<p align="center">
  Unity 6000.3.1f1 · UniTask · DOTween · TCP<br />
  Input System · uGUI / TextMeshPro · URP · UPM
</p>


# YuJanggi.Unity

Unity 기반 장기 클라이언트입니다. 게임 화면·입력·대국 흐름을 담당하며, <br>
장기 규칙과 서버 통신 계약은 공용 패키지로 분리합니다.

## Highlights

- 모드별 Flow와 GameSession을 통한 대국·종료 상태 관리
- Random / Greedy / Minimax AI 전략
- 대국 기록 기반 리플레이
- Request / Response와 서버 Event를 구분한 네트워크 처리

## Download

[Windows 설치파일 — YuJanggi v1.0.0](Downloads/YuJanggi_V1.0.0_Setup.exe?raw=true) · 약 35MB

다운로드 후 설치파일을 실행합니다. 제공된 v1.0.0 빌드와 현재 소스는 별개이며,<br> 
아래 설명은 현재 소스 기준입니다.

## Getting Started

1. **Unity 6000.3.1f1**에서 프로젝트를 엽니다.
2. Engine **2.5.1** / Protocol **1.5.2** UPM `.tgz`를 준비하고 [manifest.json](Packages/manifest.json)의 로컬 경로를 맞춥니다.
3. [BootStrapScene](Assets/Scenes/BootStrapScene.unity)을 열고 Play합니다. 초기화 후 Lobby로 이동합니다.

Local / AI는 서버 없이 진행합니다. Network는 `YuJanggi.Server`와 두 클라이언트가 필요하며, <br>
양쪽의 Engine / Protocol 버전이 Handshake 검사를 통과해야 합니다.

### 서버 주소

- `SERVER_HOST`: 환경변수가 비어 있으면 `127.0.0.1` 사용
- `SERVER_PORT`: 정수로 읽을 수 없으면 `7777` 사용

Windows 사용자 환경변수 변경 후에는 **Unity Hub와 Editor를 모두 재실행**합니다. <br>
[Bootstrap](Assets/Scripts/Bootstrap/YuJanggiBootStrap.cs)의 `Server endpoint: <host>:<port>` 로그에서 적용된 주소를 확인할 수 있습니다.

## Features

| 모드 | 대국 방식 |
| --- | --- |
| Local | 한 클라이언트에서 두 진영 입력 |
| AI | 사용자와 AI Controller의 대국 |
| Network | 서버 매칭·포진 준비 후 참가자 간 대국 |

- 기물 선택과 이동 가능 위치 표시
- 리플레이 중에도 수신 이동을 Engine에 반영해 실제 대국과 화면 탐색을 분리
- Main Lobby 복귀 전에 이전 매치 상태 초기화와 연결 해제

## Architecture

Controller는 입력·AI 결과를 전달하고, Flow는 모드별 처리 경로를 선택합니다. <br>
GameSession은 Engine과 View를 연결해 대국·리플레이·종료 상태를 관리합니다.

- **Local / AI**: LocalInGameFlow에서 Engine에 즉시 적용
- **Network**: NetworkInGameFlow에서 요청 전송 후 서버 Event를 적용
- **메시지 분배**: RequestId가 있는 응답은 RequestDispatcher로, Event는 기능별 Handler로 전달

온라인 이동은 `MovePieceResponse`의 Accepted만으로 반영하지 않고 `MovePieceEvent`를 기다립니다.<br>
종료도 로컬 Engine 결과나 `GameEndResponse`만으로 Info를 표시하지 않으며,<br>
`GameEndedEvent` 수신 후 최종 결과를 반영합니다.

## Build / CI/CD

- **Unity 빌드**: [Build Profiles](Assets/Settings/Build%20Profiles)의 Debug / Release 프로필 사용. Debug만 Development Build 활성화
- **공용 패키지**: Engine / Protocol의 Tag 기반 GitHub Actions가 UPM `.tgz`를 Artifact로 제공. 다운로드해 로컬 패키지로 설치
- **Unity 자동화**: 이 저장소에는 현재 별도 CI/CD Workflow가 없음

주요 의존성은 UniTask, Input System, uGUI / TextMeshPro, URP, DOTween입니다.<br>
Protocol의 JSON 직렬화에 필요한 DLL은 `Assets/Plugins/YuJanggiCommon`에 포함되어 있습니다.

## Project Structure

```text
Assets/
├─ Scenes/               # Bootstrap·Lobby·대국 씬
└─ Scripts/
   ├─ Bootstrap/         # 초기화와 공용 Manager
   ├─ Lobby/             # 모드별 준비와 매칭·포진 통신
   ├─ InGame/            # Controller·Flow·Session·View
   ├─ Network/           # Transport·Connection·요청 추적
   └─ Runtime/           # Input·Board·Piece·UI
Packages/                # UPM 의존성 설정
ProjectSettings/         # Unity 프로젝트 설정
Downloads/               # Windows 설치파일
```

## Limitations

- 서버의 Engine 기반 이동 검증은 아직 미구현
- 재접속 시 대국 Snapshot 복구 경로 없음
- 온라인 무르기·한 수 쉼·제한시간의 별도 서버 동기화 계약 없음

## Related Projects

- [YuJanggi.Engine](https://github.com/SeokJinYoo98/YuJanggi.Engine) — Unity 비의존 장기 규칙과 게임 상태
- [YuJanggi.Protocol](https://github.com/SeokJinYoo98/YuJanggi.Protocol) — 클라이언트·서버 공유 메시지 계약
- [YuJanggi.Server](https://github.com/SeokJinYoo98/YuJanggi.Server) — 연결·매칭·GameRoom과 서버 메시지 처리
