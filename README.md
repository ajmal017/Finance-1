# Finance

*See Wiki for additional detail

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
-Position objects represent holdings of a Security, as maintained by a collection of Trades.
-A Portfolio holds a collections of Positions, executes Trades, and provides state information for a given date.
-A Simulation contains an independent instance of a portfolio and parameters from which to execute a given strategy, along with 
functionality to analyze and present results.
-Manager objects maintain control of their respective objects (SimulationManager, PortfolioManager, TradeManager, etc)
-In a simulation, a Strategy object generates Signals for a given security and day, indicting a Buy or Sell for that security.  The Signal is processed by various parties (Risk Manager, Portfolio Manager, etc) before being actioned (approved/rejected).
-DataManager maintains control over download/storage/retrieval of price data by interfacing with an IDataProvider implementation (represents a broker or 3rd party API), and a PriceDatabase (EF Context).

Dependencies:

-Currently requires installation of Interactive Brokers LLC (IBKR) data API and trading gateway.  Some connection parameters are hard-coded for ease of development.  Only one implementation of IDataProvider at the moment which supports IBKR.

TestFormProject:

-WinForms project to launch the solution; most all controls/forms are actually defined in the base library and built at runtime.  Current efforts are directed at building this out right now.

What Works Right Now:

-All basic functions for retrieval and storage of price data from IBKR.
-All basic functions for creating and executing multiple trading strategies against a universe of securities and model portfolio.
-Error checking and correcting for common discrepancies.
-Most main components of the UI; connecting to data provider, updating, automatic updates, creating and running simulations.

Current Work:

-Migrating/redesigning additional Controls and Form views in the library to build out the UI.  Attemping to make components as modular as possible with a degree of abstraction that will allow easy building of different user views.  Continue to build out the simulation interface to provide robust results viewing.
-Create/update GitHub wiki

Future Work:

-Create and run simulation batches with automated parameter optimization.
-Port into real-time portfolio management tools to interact directly with IBKR trding system.
