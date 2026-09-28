# YuJanggi.Unity

Unity 6 기반 장기 클라이언트입니다. 로컬·AI 대국과 서버를 통한 1:1 온라인 대국에서 `YuJanggi.Engine`으로 게임을 진행합니다.

[로컬·AI·온라인 대국 화면을 빠르게 보여줄 때, 짧은 플레이 GIF]

## 프로젝트 개요

- 로비에서 대국 방식, 진영, 포진 및 제한시간 선택
- `YuJanggi.Protocol`을 사용한 서버 통신
- `YuJanggi.Engine`을 사용한 장기판과 게임 진행

## 기술 스택

- Unity 6000.3.1f1
- C#
- UniTask
- TCP
- TextMeshPro
- YuJanggi.Engine / YuJanggi.Protocol

## 주요 기능

- 로컬 2인 및 AI 대국
- 서버 연결과 Protocol·Engine 버전 Handshake
- 매치메이킹 신청·취소 및 포진 제출
- 서버 `GameReady`의 확정 포진으로 온라인 대국 준비
- 장기판 입력, 이동 가능 위치 표시 및 대국 진행
- 기록을 앞뒤로 탐색하는 리플레이 화면

## 클라이언트 구조

```text
LobbyManager → JanggiOptionStore → InGameManager
                                    ├─ GameSession → YuJanggi.Engine
                                    └─ LocalInGameFlow / NetworkInGameFlow

NetworkManager → NetworkConnection → TcpTransport
               └─ MatchingHandler / RequestDispatcher
```

## 주요 코드

| 구성 요소 | 역할 |
| --- | --- |
| [`LobbyManager`](Assets/Scripts/Lobby/LobbyManager.cs) | 대국 옵션 선택과 씬 진입 |
| [`NetworkManager`](Assets/Scripts/Bootstrap/NetworkManager.cs) | 연결·매칭 상태 관리 |
| [`NetworkConnection`](Assets/Scripts/Network/NetworkConnection.cs) | TCP 연결, Handshake 및 수신 루프 |
| [`MatchingHandler`](Assets/Scripts/Lobby/Matching/MatchingHandler.cs) | 매칭 메시지와 `GameReady` 처리 |
| [`InGameManager`](Assets/Scripts/InGame/InGameManager.cs) | 엔진·세션·화면 구성 |
| [`GameSession`](Assets/Scripts/InGame/Session/GameSession.cs) | 대국 상태와 입력 흐름 관리 |

## 실행 방법

1. Unity 6000.3.1f1로 프로젝트를 엽니다.
2. `Packages/manifest.json`의 Engine·Protocol 로컬 패키지 경로가 현재 컴퓨터의 저장소 위치를 가리키는지 확인합니다.
3. [`BootStrapScene`](Assets/Scenes/BootStrapScene.unity)을 실행합니다. 초기화 후 `LobbyScene`으로 이동합니다.
4. 로컬 또는 AI 대국을 선택합니다. 온라인 대국은 `YuJanggi.Server.V2`를 실행한 뒤 로비에서 서버에 연결합니다.

기본 서버 주소는 `YuJanggiBootStrap`에 설정된 `127.0.0.1:7777`입니다.

## 관련 프로젝트

- `YuJanggi.Server.V2`: 온라인 대국 서버
- `YuJanggi.Engine`: 장기 규칙과 게임 진행
- `YuJanggi.Protocol`: 통신 메시지와 DTO

## 포트폴리오

[게임 플레이와 상세 설계 과정을 소개할 때, 포트폴리오 링크]
