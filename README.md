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

- GameMode의 게임 진행과 Live / Replay State의 화면 처리 분리
- 기보 보기 중에도 실제 대국은 계속 진행되며, GameMode가 턴에 따른 입력 흐름을 관리합니다.
- State가 실제 대국을 보여주는 Live 화면과 과거 기보를 보여주는 Replay 화면을 전환합니다.
- Random / Greedy / Minimax AI 전략 구현
- RequestId 기반 응답 추적과 서버 Event별 Handler 분배

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
| AI | Local Player와 AI Player 대국 |
| Network | 서버 매칭·포진 준비 후 참가자 간 대국 |

- 기물 선택과 이동 가능 위치 표시
- 리플레이 중에도 수신 이동을 Engine에 반영해 실제 대국과 화면 탐색을 분리
- Main Lobby 복귀 전에 이전 매치 상태 초기화와 연결 해제
- 
## Architecture

게임 진행은 `GameMode`, Live / Replay의 보드 표현과 기보 탐색은 `InGameState`가 담당합니다. <br>`InGameManager`는 객체 생성, 생명주기, Engine 이벤트 연결과 상태 전환을 관리합니다.

- **입력 처리**: InputHandler가 입력을 받고, LocalPlayer가 기물 선택과 이동 요청을 해석합니다.
- **게임 진행**: LocalMode가 Engine에 명령을 전달하고 다음 턴의 입력 대상을 설정합니다.
- **화면 표현**: LiveState는 실제 이동을 표시하고, ReplayState는 선택한 기록을 재생합니다. 기보 보기 중에도 라이브 UI 갱신은 유지합니다.
- **입력 제어**: State 진입 시 GameMode를 통해 로컬 입력을 정지하거나 재개합니다.
- **메시지 분배**: NetworkManager가 RequestId가 있는 응답을 RequestDispatcher로, 서버 Event를 기능별 Handler로 전달합니다.

## Project Structure

```text
Assets/
├─ Scenes/                  # Bootstrap·Login·Lobby·대국 씬
└─ Scripts/
   ├─ Bootstrap/            # 애플리케이션 초기화와 NetworkManager
   ├─ Core/                 # 입력·Player·State·AI 공통 계약
   ├─ Lobby/                # 모드별 준비 패널과 매칭·포진 통신
   ├─ InGame/
   │  ├─ GameMode/          # 게임 진행과 명령 처리
   │  ├─ Input/             # 포인터·JSON 기보 입력
   │  ├─ Player/            # 기물 선택과 이동 입력 해석
   │  ├─ State/             # Live·Replay 화면 상태와 기보 재생
   │  ├─ Handler/           # 인게임 네트워크 Handler
   │  └─ Views/             # 보드·기물·이동 가이드·효과·UI
   ├─ Network/              # TCP 통신·요청 추적·Protocol 변환
   ├─ AI/                   # 이동 탐색과 평가 전략
   ├─ Audio/                # 오디오 관리
   └─ UI/                   # 공통 버튼·볼륨·표시 제어
Packages/                   # UPM 의존성 설정
ProjectSettings/            # Unity 프로젝트 설정
```

## Build / CI/CD

- **Unity 빌드**: [Build Profiles](Assets/Settings/Build%20Profiles)의 Debug / Release 프로필 사용. Debug만 Development Build 활성화
- **공용 패키지**: Engine / Protocol의 Tag 기반 GitHub Actions가 UPM `.tgz`를 Artifact로 제공. 다운로드해 로컬 패키지로 설치
- **Unity 자동화**: 이 저장소에는 현재 별도 CI/CD Workflow가 없음

주요 의존성은 UniTask, Input System, uGUI / TextMeshPro, URP, DOTween입니다.<br>
Protocol의 JSON 직렬화에 필요한 DLL은 `Assets/Plugins/SystemTextJson`에 포함되어 있습니다.

## Limitations

- 서버의 Engine 기반 이동 검증은 아직 미구현
- 재접속 시 대국 Snapshot 복구 경로 없음
- 온라인 무르기·한 수 쉼·제한시간의 별도 서버 동기화 계약 없음

## Related Projects

- [YuJanggi.Engine](https://github.com/SeokJinYoo98/YuJanggi.Engine) — Unity 비의존 장기 규칙과 게임 상태
- [YuJanggi.Protocol](https://github.com/SeokJinYoo98/YuJanggi.Protocol) — 클라이언트·서버 공유 메시지 계약
- [YuJanggi.Server](https://github.com/SeokJinYoo98/YuJanggi.Server) — 연결·매칭·GameRoom과 서버 메시지 처리
