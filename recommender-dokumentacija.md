# WildNest recommendation and sentiment

This note describes the code that is in the repository. It does not describe an earlier design.

## Activity recommendations

The recommender is an ML.NET `MatrixFactorization` model. It is trained only on activity reservations.

A training row is one distinct visitor and one activity. The reservation counts only when its status is Confirmed or Completed. Each such pair is a positive interaction with label `1`. Reviews, ratings, animal, category, and adoption are not training rows and are not added to the score.

Training runs in `RecommendationTrainer`. It does not run inside a recommendation request. `RecommendationTrainingHostedService` trains once when the API process starts, then checks again every `RECOMMENDATION_TRAINING_CHECK_MINUTES` (default 5). It trains again when `RECOMMENDATION_RETRAIN_INTERVAL_MINUTES` have passed (default 360) or when at least `RECOMMENDATION_RETRAIN_AFTER_NEW_RESERVATIONS` new confirmed or completed reservations exist since the last attempt (default 10). An administrator can train immediately with `POST /api/Recommendation/retrain`.

The model stays in memory. A new process trains again from the database. If training throws, the previous model is kept when one exists.

Matrix factorization is skipped, and the cache stays empty, unless there are at least 4 distinct pairs, at least 2 visitors, and at least 2 activities. A non-finite score also clears the model. In those cases suggestions use popularity.

### Who sees a personalised list

`GET /api/Recommendation` reads the signed-in user from the JWT. `limit` defaults to 5 and must be from 1 to 20.

Candidates are active activities that still have an active schedule today (not yet started) or on a later day. Activities the caller has already reserved, except a cancelled reservation, are left out.

The caller gets matrix-factorization scores only when all of these are true:

- a model is in the cache
- the caller has at least one distinct confirmed or completed activity
- that caller's user id was in the training rows

Otherwise the list is the popularity fallback. The same fallback is used when the model returns no usable scores.

Popularity is the number of confirmed and completed reservations for that activity. Activities with a count of zero are omitted. The highest counts come first.

### Reasons shown to the user

Each suggestion has one reason.

| Situation | Code | Text |
| --- | --- | --- |
| Personalised score | `SimilarReservations` | Visitors with similar reservations also booked this. |
| Popularity fallback | `Popular` | Popular with visitors. |

The mobile home screen shows that sentence on the recommendation tile.

### How to demonstrate it

1. Start the stack and wait until the API log says recommendation training finished and the model is ready. The demo seed has enough confirmed and completed reservations for that.
2. Sign in on the phone as `Customer` / `test`. That account has no reservations. The home recommendations use popularity and the reason "Popular with visitors."
3. Sign out and sign in as `mobile` / `test`. That account has completed and confirmed reservations. After training, the home recommendations use the model and the reason "Visitors with similar reservations also booked this."
4. As `desktop` / `test`, call `POST /api/Recommendation/retrain` (or wait for the background job) after new reservations. The next home refresh uses the new model.

## Review sentiment

Sentiment is a separate pre-trained binary model. It labels a review positive or negative. It is not the recommendation model, and the API does not train it at startup.

### How it was trained

The trainer is `eCommerce/tools/SentimentModelTrainer`. The pipeline is the ML.NET binary sentiment tutorial: `FeaturizeText` followed by `SdcaLogisticRegression`. The split is a stratified 20% hold-out with seed 1. The saved zip was fit on the training split only.

The training text is the UCI Sentiment Labelled Sentences set (3,000 sentences from Amazon, IMDb, and Yelp) plus 56 short zoo reviews in `eCommerce/tools/SentimentModelTrainer/Data/zoo_reviews.tsv` (English and Bosnian). The zoo file is extra in-domain text. It is not a substitute for the UCI set.

Held-out metrics, from `eCommerce/eCommerce.Services/Sentiment/Models/review-sentiment.metrics.txt`:

| Measure | Value |
| --- | --- |
| Train rows | 2,444 (1,222 positive, 1,222 negative) |
| Test rows | 612 (306 positive, 306 negative) |
| Hold-out accuracy | 80.88% |
| Hold-out AUC | 89.70% |
| Hold-out F1 | 81.40% |
| Zoo rows in the hold-out | 11, of which 9 were correct (81.82%) |

### Dataset source and licence

Source: [UCI Sentiment Labelled Sentences](https://archive.ics.uci.edu/dataset/331/sentiment+labelled+sentences), DOI `10.24432/C57604`.

Citation: Dimitrios Kotzias, Misha Denil, Nando de Freitas, and Padhraic Smyth. 2015. From Group to Individual Labels using Deep Features. KDD 2015.

Files: `amazon_cells_labelled.txt`, `imdb_labelled.txt`, `yelp_labelled.txt` (sentence, tab, 0 or 1).

Licence: Creative Commons Attribution 4.0 International (CC BY 4.0), as stated on the UCI dataset page. Attribute Kotzias et al., KDD 2015.

### Where the file lives and how it is loaded

The model file in the repository is `eCommerce/eCommerce.Services/Sentiment/Models/review-sentiment.zip`. The project copies it to `ml-models/review-sentiment.zip` beside the API.

`SentimentService.EnsureReadyAsync` loads that zip once. `Program.cs` calls it before the API accepts traffic, then backfills sentiment labels on reviews that do not have one. Prediction is serialized because `PredictionEngine` is not thread-safe.

`SENTIMENT_MODEL_PATH` can point at a different file. When the file is missing, the API still starts, new reviews stay unlabeled, and the log says the model is not available.
