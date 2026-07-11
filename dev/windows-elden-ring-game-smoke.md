# Elden Ring recompressed animation smoke test

Use a disposable mod directory and a backed-up save. Never overwrite the game
installation's original archive.

1. Start from the validated vanilla `chr/c0000.anibnd.dcx` fixture.
2. Unpack it with the macOS release using Wine and the game-owned
   `oo2core_6_win64.dll`.
3. Repack the unchanged extracted folder with `--oodle-native-compression`.
4. Run `dev/validate-oodle-backends.sh` and retain the 655-file manifest before
   moving the archive to Windows.
5. Install the recompressed archive through the normal mod loader workflow,
   with anti-cheat disabled and offline mode enabled.
6. Launch the same Elden Ring version from which the fixture and DLL came.
7. Load into a safe test area, move, sprint, roll, jump, attack with one light
   and one heavy weapon, two-hand a weapon, and rest at a grace.
8. Exit normally and inspect the game and mod-loader logs for archive,
   animation, decompression, or checksum failures.
9. Restore the original mod state and confirm a second launch succeeds.

The gate passes only if the game loads the archive, all listed player
animations execute visibly, no relevant errors appear in logs, and restoring
the original archive succeeds. Record game version, executable hash, mod-loader
version, recompressed archive SHA-256, tester, date, and result in the release
notes.
