namespace AppointmentBooking.Mobile.Utilities;

/// <summary>
/// Scrolls the vertical scroll containers of a page back to the top.
/// Horizontal lists and lists inside a ScrollView are left as they are.
/// </summary>
public static class ScrollPositionReset
{
    public static void ResetToTop(Page? page)
    {
        if (page is null)
        {
            return;
        }

        ResetChildren(page);
    }

    private static void ResetChildren(IVisualTreeElement element)
    {
        foreach (IVisualTreeElement child in element.GetVisualChildren())
        {
            if (child is ScrollView scrollView)
            {
                ResetScrollView(scrollView);
            }
            else if (child is CollectionView collectionView)
            {
                ResetCollectionView(collectionView);
            }
            else
            {
                ResetChildren(child);
            }
        }
    }

    private static void ResetScrollView(ScrollView scrollView)
    {
        if (scrollView.Orientation is ScrollOrientation.Horizontal or ScrollOrientation.Neither
            || scrollView.ScrollY <= 0)
        {
            return;
        }

        _ = scrollView.ScrollToAsync(scrollView.ScrollX, 0, false);
    }

    private static void ResetCollectionView(CollectionView collectionView)
    {
        if (!collectionView.IsVisible
            || collectionView.IsGrouped
            || collectionView.ItemsLayout is ItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal })
        {
            return;
        }

        object? firstItem = collectionView.ItemsSource?.Cast<object>().FirstOrDefault();

        if (firstItem is null)
        {
            return;
        }

        collectionView.ScrollTo(firstItem, position: ScrollToPosition.Start, animate: false);
    }
}
