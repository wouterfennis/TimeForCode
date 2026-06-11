Feature: Project Detail Page
    As a visitor
    I want to view the details of a specific project
    So that I can learn more about it before deciding to contribute

Scenario: Visitor navigates to a known project detail page
    Given A project exists and is published
    When The visitor navigates to the project detail page
    Then The project heading is visible
    And A back link to the projects list is visible
