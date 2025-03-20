# TimeForCode

[![Continuous Integration](https://github.com/wouterfennis/TimeForCode/actions/workflows/build.yaml/badge.svg)](https://github.com/wouterfennis/TimeForCode/actions/workflows/build.yaml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=wouterfennis_TimeForCode&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=wouterfennis_TimeForCode)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=wouterfennis_TimeForCode&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=wouterfennis_TimeForCode)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=wouterfennis_TimeForCode&metric=coverage)](https://sonarcloud.io/summary/new_code?id=wouterfennis_TimeForCode)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=wouterfennis_TimeForCode&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=wouterfennis_TimeForCode)

Welcome to **TimeForCode**, an initiative aimed at connecting companies and
 individuals willing to donate their time and skills to open-source projects.
  This repository contains the software that powers the **TimeForCode**
   platform, including the website, backend, and supporting documentation.

## Table of Contents

- [About the Project](#about-the-project)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
  - [Code of Conduct](#code-of-conduct)
  - [How to Contribute](#how-to-contribute)
- [License](#license)
- [Contact](#contact)

---

## About the Project

**TimeForCode** is a platform designed to foster collaboration between
 companies and the open-source community. Organizations can donate developer
  hours to open-source projects, empowering teams to work on impactful
   initiatives while gaining valuable experience.

This repository houses:

- The **website** where companies can register and pledge hours.
- The **backend** API that manages project listings, donations, and participant
 information.
- The **documentation** for contributors, API usage, and general guidelines.

---

## Features

- **Company and Individual Accounts**: Register as a company or an individual
 donor.
- **Project Listings**: Browse open-source projects in need of contributions.
- **Hour Donations**: Companies and individuals can pledge development hours
 to specific projects.
- **Collaboration Tools**: Built-in tools for time tracking and reporting
 on contributions.

---

## Tech Stack

- **Frontend**: T.B.D.
- **Backend**: T.B.D.
- **Database**: T.B.D.
- **Authentication**: OAuth 2.0, JWT
- **Documentation**: Markdown, Swagger for API documentation
- **Deployment**: CI/CD with GitHub Actions

---

## Getting Started

### Prerequisites

- Running Docker

### Installation

```powershell
docker compose up --build
```

## Usage

1. Navigate to [http://localhost:8082] to visit the website

## Contributing

We welcome contributions to TimeForCode! Please take a moment to review our
 guidelines to help us keep this project accessible and collaborative.

## Code of Conduct

By participating in this project, you agree to abide by the Code of Conduct.
 Please be respectful and constructive in your interactions with others.

### How to Contribute

Fork the repository.
Create a new feature branch (git checkout -b feature/AmazingFeature).
Commit your changes (git commit -m 'Add some AmazingFeature').
Push to the branch (git push origin feature/AmazingFeature).
Open a Pull Request.
Before submitting your PR, make sure to:

Your branch passes the automated sanity checks and tests
Ensure your code adheres to the style guide.
Document any new functionality or endpoints.

## License

Distributed under the MIT License. See LICENSE for more information.

## Contact

If you have any questions or suggestions, feel free to contact the project
 maintainers:

Name: Wouter Fennis
GitHub: @wouterfennis

Happy coding, and thank you for contributing to open-source with TimeForCode!
