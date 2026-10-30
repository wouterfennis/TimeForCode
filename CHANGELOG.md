# Changelog

## 1.0.0 (2026-10-30)


### Features

* add donor organization CRUD admin page ([#65](https://github.com/wouterfennis/TimeForCode/issues/65)) ([#67](https://github.com/wouterfennis/TimeForCode/issues/67)) ([6369d5f](https://github.com/wouterfennis/TimeForCode/commit/6369d5fd6be7401aabbdb7a099dc17229f761c12))
* **api:** declare and enforce input limits in the OpenAPI documents ([#98](https://github.com/wouterfennis/TimeForCode/issues/98)) ([ef82b86](https://github.com/wouterfennis/TimeForCode/commit/ef82b863b3ee110b5c72823d1802e8796bd22955))
* **authorization:** fail fast on missing signing key, publish kid and support rotation ([#106](https://github.com/wouterfennis/TimeForCode/issues/106)) ([#119](https://github.com/wouterfennis/TimeForCode/issues/119)) ([1b54e89](https://github.com/wouterfennis/TimeForCode/commit/1b54e89f28d145fae2efd644bb035b95f337e494))
* **authorization:** harden refresh tokens, logout and admin refresh ([#104](https://github.com/wouterfennis/TimeForCode/issues/104)) ([#117](https://github.com/wouterfennis/TimeForCode/issues/117)) ([77b3a2e](https://github.com/wouterfennis/TimeForCode/commit/77b3a2e069ad6349931ce85697cbc8b5c778aba7))
* **authorization:** let users delete their account and document personal data ([#107](https://github.com/wouterfennis/TimeForCode/issues/107)) ([#122](https://github.com/wouterfennis/TimeForCode/issues/122)) ([afb823d](https://github.com/wouterfennis/TimeForCode/commit/afb823de8b53ef146d6291e56c00978b2af176eb))
* **authorization:** make OAuth login state durable, single-use and PKCE-protected ([#105](https://github.com/wouterfennis/TimeForCode/issues/105)) ([#118](https://github.com/wouterfennis/TimeForCode/issues/118)) ([72a1bd6](https://github.com/wouterfennis/TimeForCode/commit/72a1bd61302f1dbc6326d6dce3e6ff50376f35f8))
* **authorization:** stop storing GitHub access tokens and share encryption keys ([#103](https://github.com/wouterfennis/TimeForCode/issues/103)) ([#115](https://github.com/wouterfennis/TimeForCode/issues/115)) ([6ec8fa0](https://github.com/wouterfennis/TimeForCode/commit/6ec8fa031e42f331417fecdef09373d8d10215ef))
* **authorization:** test-only admin access on the local stack ([#139](https://github.com/wouterfennis/TimeForCode/issues/139)) ([#143](https://github.com/wouterfennis/TimeForCode/issues/143)) ([7ea1095](https://github.com/wouterfennis/TimeForCode/commit/7ea10956ff9d969d8e4027cdbe07742511d02a25))
* **donation:** [@live](https://github.com/live) Donation scenarios against the real stack ([#137](https://github.com/wouterfennis/TimeForCode/issues/137)) ([#145](https://github.com/wouterfennis/TimeForCode/issues/145)) ([0125c69](https://github.com/wouterfennis/TimeForCode/commit/0125c69995db27eee712af279807f3686f681d7f))
* **donation:** contributor registration and skill profile (back-end) ([#77](https://github.com/wouterfennis/TimeForCode/issues/77)) ([#134](https://github.com/wouterfennis/TimeForCode/issues/134)) ([138b7b3](https://github.com/wouterfennis/TimeForCode/commit/138b7b3094ca7f0d53bed7fcf0ac56bdc33ed3a0))
* **donation:** correct or delete logged time contributions ([#100](https://github.com/wouterfennis/TimeForCode/issues/100)) ([#173](https://github.com/wouterfennis/TimeForCode/issues/173)) ([b0577bb](https://github.com/wouterfennis/TimeForCode/commit/b0577bb7d9e375bc479574875b14b1c311f65088))
* **donation:** log and list time contributions on projects ([#66](https://github.com/wouterfennis/TimeForCode/issues/66)) ([#109](https://github.com/wouterfennis/TimeForCode/issues/109)) ([d9b8561](https://github.com/wouterfennis/TimeForCode/commit/d9b85611cb173cf2f446ea70ba04bc2c8fd01cb1))
* **donation:** maintainer profile and project ownership ([#79](https://github.com/wouterfennis/TimeForCode/issues/79)) ([#156](https://github.com/wouterfennis/TimeForCode/issues/156)) ([bceca8e](https://github.com/wouterfennis/TimeForCode/commit/bceca8e1d930754bef4a059ba6007a8cafffd62c))
* **donation:** search and filter active projects ([#76](https://github.com/wouterfennis/TimeForCode/issues/76)) ([#97](https://github.com/wouterfennis/TimeForCode/issues/97)) ([e1e5404](https://github.com/wouterfennis/TimeForCode/commit/e1e54044ac0c97fafba03315a8149089bbc3fedd))
* expand agent workflow with skills, prompts, agents, and governance instructions ([#47](https://github.com/wouterfennis/TimeForCode/issues/47)) ([20ef0e5](https://github.com/wouterfennis/TimeForCode/commit/20ef0e515472e9ddef99de2c61b3142a2afb8105))
* **frontend:** scaffold the React SPA with lint, type-check and tests in CI ([#110](https://github.com/wouterfennis/TimeForCode/issues/110)) ([#116](https://github.com/wouterfennis/TimeForCode/issues/116)) ([d414c4e](https://github.com/wouterfennis/TimeForCode/commit/d414c4e96d22a81eafc671325c2b8ee9882df491))
* **frontend:** ship the nginx front-end image with security headers and smoke checks ([#111](https://github.com/wouterfennis/TimeForCode/issues/111)) ([#146](https://github.com/wouterfennis/TimeForCode/issues/146)) ([3aa8bcd](https://github.com/wouterfennis/TimeForCode/commit/3aa8bcd0fc1eda76a5d5c2cf3efaf8b72d04b18b))
* structured logging, FluentValidation pipeline, and Authorization API v1 routes ([#59](https://github.com/wouterfennis/TimeForCode/issues/59)) ([23ae3c5](https://github.com/wouterfennis/TimeForCode/commit/23ae3c5d6396d589aff7ff3890e86ac556e865b2))
* **testing:** live host for Reqnroll specs with a [@live](https://github.com/live) tag ([#135](https://github.com/wouterfennis/TimeForCode/issues/135)) ([#144](https://github.com/wouterfennis/TimeForCode/issues/144)) ([4e6e4e3](https://github.com/wouterfennis/TimeForCode/commit/4e6e4e334580166a802ba8ecf930c48244407dce))


### Bug Fixes

* **authorization:** resolve Sonar code smells ([#127](https://github.com/wouterfennis/TimeForCode/issues/127)) ([#129](https://github.com/wouterfennis/TimeForCode/issues/129)) ([15fb864](https://github.com/wouterfennis/TimeForCode/commit/15fb864a42960921780f8f4cf85363f0ef913302))
* **test:** live publish scenario asserts on the shared project id ([#161](https://github.com/wouterfennis/TimeForCode/issues/161)) ([2f2dca4](https://github.com/wouterfennis/TimeForCode/commit/2f2dca456b1df77019a6b8f9715e89bc6243817f))
* update SonarCloud coverage exclusions to include src/Website dir… ([#42](https://github.com/wouterfennis/TimeForCode/issues/42)) ([8231723](https://github.com/wouterfennis/TimeForCode/commit/82317231dde3d84e21178beea4531a13d273246b))
