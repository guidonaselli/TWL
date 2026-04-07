for branch in $(git branch -r | grep -E "origin/jules/|origin/gsd/"); do
  timestamp=$(git log -1 --format=%ct $branch)
  now=$(date +%s)
  days=$(( (now - timestamp) / 86400 ))
  echo "$branch: $days days"
done
