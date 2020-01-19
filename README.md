# Finance

*See Wiki for overview of project

Dependencies:

-Currently requires installation of Interactive Brokers LLC (IBKR) data API and trading gateway.  Some connection parameters are hard-coded for ease of development.  Only one implementation of IDataProvider at the moment which supports IBKR.

TestFormProject:

-WinForms project to launch the solution; most all controls/forms are actually defined in the base library and built at runtime.

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
