## ADDED Requirements

### Requirement: Recognise "Up or Down" directional market patterns
The `QuestionParser` SHALL recognise market questions that ask whether a cryptocurrency price will go up or down, and return a `DirectionalResult` with the extracted token symbol and direction keywords.

#### Scenario: Standard "Up or Down" pattern
- **WHEN** the question is "Will Bitcoin go Up or Down between 9:45AM-10:00AM ET?"
- **THEN** the parser SHALL return a `DirectionalResult` with Symbol = "BTC" and direction indicators for both Up and Down

#### Scenario: "Higher or Lower" variant
- **WHEN** the question is "Will ETH be Higher or Lower at 10:00AM ET?"
- **THEN** the parser SHALL return a `DirectionalResult` with Symbol = "ETH"

#### Scenario: Case-insensitive matching
- **WHEN** the question contains "up or down" in any casing (e.g., "UP OR DOWN", "Up Or Down")
- **THEN** the parser SHALL recognise the pattern and return a `DirectionalResult`

#### Scenario: Non-directional question with no target price
- **WHEN** the question is "Will Bitcoin do something?" with no directional or price-target patterns
- **THEN** the parser SHALL return an `UnrecognisedResult`

### Requirement: Extract time windows from directional market questions
The `QuestionParser` SHALL extract start and end times from directional market question text when a time window is present.

#### Scenario: Full time window with AM/PM and timezone
- **WHEN** the question contains "9:45AM-10:00AM ET"
- **THEN** the parser SHALL return `WindowStart = 09:45` and `WindowEnd = 10:00` and `Timezone = "ET"`

#### Scenario: No time window present
- **WHEN** the question is "Will Bitcoin go Up or Down?" with no time information
- **THEN** the parser SHALL return null for `WindowStart` and `WindowEnd`

#### Scenario: Time window with different AM/PM periods
- **WHEN** the question contains "11:30AM-12:30PM EST"
- **THEN** the parser SHALL return `WindowStart = 11:30` and `WindowEnd = 12:30` and `Timezone = "EST"`

### Requirement: Return structured ParseResult discriminated union
The `QuestionParser.Parse` method SHALL return a `ParseResult` sealed hierarchy instead of the current `(string?, double?)` tuple, supporting price-target, directional, and unrecognised market types.

#### Scenario: Price-target market question
- **WHEN** the question is "Will Bitcoin hit $100K?"
- **THEN** the parser SHALL return a `PriceTargetResult` with Symbol = "BTC" and TargetPrice = 100000

#### Scenario: Directional market question
- **WHEN** the question is "Bitcoin Up or Down 9:45AM-10:00AM ET?"
- **THEN** the parser SHALL return a `DirectionalResult` with Symbol = "BTC" and parsed time window

#### Scenario: Unparseable question
- **WHEN** the question cannot be matched to any known pattern
- **THEN** the parser SHALL return an `UnrecognisedResult`
