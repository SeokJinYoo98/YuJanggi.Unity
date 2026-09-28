# YuJanggi.Unity

Unity 기반 1:1 온라인 장기 클라이언트입니다.

## 프로젝트 개요

- Unity 기반 장기 클라이언트
- YuJanggi.Server와 TCP 통신
- YuJanggi.Engine 기반 게임 진행
- 매칭 / 포진 선택 / 게임 진행 / 리플레이 지원

## 게임 플레이

![Gameplay](./Docs/Images/gameplay.gif)

[Gameplay Video](링크)

## 기술 스택

- Unity 6
- C#
- UniTask
- TCP
- TextMeshPro
- YuJanggi.Engine
- YuJanggi.Protocol

## 클라이언트 구조

    UI / View
        ↓
    Controller
        ↓
    Service
        ↓
    NetworkConnection
        ↓
    RequestDispatcher
        ↓
    TcpTransport

    Controller
        ↓
    YuJanggi.Engine

## 주요 기능

- 서버 연결 및 Handshake
- 매치메이킹
- 매칭 취소
- 포진 선택
- 포진 자동 제출
- GameReady 수신
- 게임 세션 생성
- 장기 게임 진행
- 기보 저장
- 리플레이

## 주요 코드

| 구성 요소 | 역할 |
| --- | --- |
| `NetworkConnection` | 서버 연결 및 Receive Loop |
| `RequestDispatcher` | Request / Response 처리 |
| `TcpTransport` | TCP 송수신 |
| `MatchingHandler` | 매칭 관련 메시지 처리 |
| `MatchingService` | 매칭 상태 관리 |
| `NetworkManager` | 네트워크 상태 및 세션 정보 관리 |
| `LobbyManager` | 로비 및 매칭 흐름 제어 |
| `GameController` | 게임 진행 제어 |
| `BoardController` | 보드 입력 및 화면 연결 |
| `GameSession` | 현재 게임 세션 정보 관리 |

## 실행 방법

### 요구 사항

- Unity 6
- YuJanggi.Server

### 실행

1. YuJanggi.Server를 실행합니다.
2. Unity 프로젝트를 실행합니다.
3. Lobby Scene을 실행합니다.
4. 서버에 연결합니다.
5. 매치메이킹을 시작합니다.

## 관련 프로젝트

- [YuJanggi.Server](링크)
- [YuJanggi.Engine](링크)
- [YuJanggi.Protocol](링크)

## 포트폴리오

- [YuJanggi 포트폴리오](노션 링크)