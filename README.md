# Finance

Full-featured trading and strategy back-testing library.

Main Functionality:
  1) Interface with supported data providers (broker APIs, public websites, etc) to retrieve, organize, and store time-series data
  for financial instruments.
  2) Implement a robust back-testing engine which can accurately model a portfolio environment for a given broker and execute trades
  based on pre-determined criteria and strategies.
  3) Provide extensibility for integration into a UI as well as eventual automation support.


This library/WinForms project is my attempt to build a completely bespoke trading platform and has served as my way of learning
and implementing new techniques/models/functionality in C#.  End-state goal is a product which allows me to implement and test various
security trading strategies against a realistic representation of my current broker (Interactive Brokers).  The design provides enough
abstraction to allow for simple transition to other data providers or executing brokers, depending on future needs.

Bottom-Up overview:

-PriceBar data is at the core of the application, with each PriceBar representing one day's data for a particular Security.
-Security objects represent a single financial instrument (just equities for now), and contain a collection of PriceBars
