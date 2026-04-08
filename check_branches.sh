for branch in origin/jules/* origin/gsd/*; do echo "$branch: $(git log -1 --format=%cr $branch)"; done
